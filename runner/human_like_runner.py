#!/usr/bin/env python3
"""
Local WebSocket server for Argus Unity Test mode + human-like scenario driver.

Unity connects outbound to ws://127.0.0.1:7777/sdk/state — this script listens on that port.
"""

from __future__ import annotations

import argparse
import asyncio
import json
import os
import subprocess
import random
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass, field
from typing import Any

import websockets

PERSONA_DELAY = {
    "explorer": (0.12, 0.35),
    "newcomer": (0.35, 0.9),
    "speedrunner": (0.02, 0.08),
    "destructor": (0.05, 0.2),
    "exploiter": (0.04, 0.15),
}


def _jitter(persona: str) -> float:
    lo, hi = PERSONA_DELAY.get(persona, (0.1, 0.3))
    return random.uniform(lo, hi)


def _parse_legal_actions(snap: dict[str, Any]) -> list[dict[str, Any]]:
    raw = snap.get("legal_actions")
    if isinstance(raw, list):
        return [x for x in raw if isinstance(x, dict)]
    if isinstance(raw, str):
        try:
            v = json.loads(raw)
            return [x for x in v if isinstance(x, dict)]
        except json.JSONDecodeError:
            return []
    return []


def _pick_action(
    scenario: str,
    legal: list[dict[str, Any]],
    step: int,
    recent_actions: list[str],
) -> dict[str, Any] | None:
    if not legal:
        return None

    def first(kind: str) -> dict[str, Any] | None:
        for a in legal:
            if a.get("action") == kind:
                return dict(a)
        return None

    def novelty_choice() -> dict[str, Any]:
        key = lambda a: json.dumps(a, sort_keys=True)
        fresh = [a for a in legal if key(a) not in set(recent_actions[-12:])]
        pool = fresh if fresh else legal
        return dict(random.choice(pool))

    if scenario == "first_turn_smoke":
        for pref in ("draw_cards", "select_card", "place_card", "move_selected_to", "move_pawn", "end_turn"):
            a = first(pref)
            if a:
                return a
        return dict(legal[0])

    if scenario == "card_placement":
        for pref in ("select_card", "place_card", "draw_cards"):
            a = first(pref)
            if a:
                return a
        return novelty_choice()

    if scenario == "pawn_movement":
        for pref in ("select_pawn", "move_selected_to", "move_pawn", "end_turn"):
            a = first(pref)
            if a:
                return a
        return novelty_choice()

    if scenario == "pawn_attack":
        for pref in ("attack_with_pawn", "attack_all", "select_pawn"):
            a = first(pref)
            if a:
                return a
        return novelty_choice()

    if scenario == "game_over_path":
        for pref in ("attack_all", "attack_with_pawn", "end_turn", "select_pawn", "move_selected_to"):
            a = first(pref)
            if a:
                return a
        return novelty_choice()

    if scenario == "ten_turn_soak":
        return novelty_choice()

    if scenario == "invalid_stress":
        for a in legal:
            if a.get("action") in ("attack_all", "attack_with_pawn", "end_turn"):
                return dict(a)
        return dict(random.choice(legal))

    if scenario in ("soak", "exploratory"):
        return novelty_choice()

    return novelty_choice()


@dataclass
class SessionLog:
    events: list[dict[str, Any]] = field(default_factory=list)
    bug_candidates: list[dict[str, Any]] = field(default_factory=list)
    actions_applied: list[str] = field(default_factory=list)
    seq: int = 0
    t0: float = field(default_factory=time.time)
    last_hash: str | None = None
    stagnant: int = 0
    perf_bug_filed: bool = False

    def add_observation(self, snap: dict[str, Any]) -> None:
        self.seq += 1
        gs = snap.get("game_state")
        if isinstance(gs, str):
            try:
                gs = json.loads(gs)
            except json.JSONDecodeError:
                gs = {}
        payload = {
            "seq": snap.get("seq"),
            "state_hash": snap.get("state_hash"),
            "scene": snap.get("scene"),
            "turn": (gs or {}).get("turn") if isinstance(gs, dict) else None,
            "game_over": (gs or {}).get("game_over") if isinstance(gs, dict) else None,
            "multimodal": snap.get("multimodal"),
        }
        self.events.append({"seq": self.seq, "kind": "observation", "payload": payload})

        mm = snap.get("multimodal") if isinstance(snap.get("multimodal"), dict) else {}
        if (
            not self.perf_bug_filed
            and isinstance(mm, dict)
            and int(mm.get("spike_count", 0)) > 200
        ):
            self.perf_bug_filed = True
            self.bug_candidates.append(
                {
                    "title": "Performance oracle: sustained frame spikes",
                    "description": "Multimodal oracle reported high spike_count during the session.",
                    "category": "performance",
                    "severity": "medium",
                    "state_hash": snap.get("state_hash"),
                    "repro_steps": {"semantic_trace": list(self.actions_applied[-30:])},
                }
            )

    def add_action(self, action: dict[str, Any]) -> None:
        self.seq += 1
        self.actions_applied.append(json.dumps(action, sort_keys=True))
        self.events.append({"seq": self.seq, "kind": "selected_action", "payload": {"action": action}})

    def note_stagnation(self, h: str | None) -> None:
        if h and h == self.last_hash:
            self.stagnant += 1
        else:
            self.stagnant = 0
        self.last_hash = h
        if self.stagnant >= 25:
            self.bug_candidates.append(
                {
                    "title": "Possible soft-lock (state hash stagnant)",
                    "description": "Observed 25+ identical state_hash values while legal actions remained available.",
                    "category": "hang",
                    "severity": "medium",
                    "state_hash": h,
                    "repro_steps": {"semantic_trace": list(self.actions_applied[-40:])},
                }
            )
            self.stagnant = 0


async def handle_connection(websocket: Any, scenario: str, persona: str, max_steps: int, log: SessionLog) -> None:
    steps = 0
    async for message in websocket:
        try:
            snap = json.loads(message)
        except json.JSONDecodeError:
            continue
        log.add_observation(snap)
        log.note_stagnation(snap.get("state_hash"))
        legal = _parse_legal_actions(snap)
        gs = snap.get("game_state")
        if isinstance(gs, str):
            try:
                gs = json.loads(gs)
            except json.JSONDecodeError:
                gs = {}
        if isinstance(gs, dict) and gs.get("game_over"):
            break

        if steps >= max_steps:
            break

        action = _pick_action(scenario, legal, steps, log.actions_applied)
        if not action:
            await asyncio.sleep(_jitter(persona))
            continue

        if action.get("action") == "wait":
            await asyncio.sleep(float(action.get("seconds", 0.1)))
            steps += 1
            continue

        log.add_action(action)
        await websocket.send(json.dumps(action))
        steps += 1
        await asyncio.sleep(_jitter(persona))


async def main_async(args: argparse.Namespace) -> SessionLog:
    log = SessionLog()

    async with websockets.serve(
        lambda ws: handle_connection(ws, args.scenario, args.persona, args.max_steps, log),
        args.host,
        args.port,
        max_size=8 * 1024 * 1024,
    ):
        print(f"[human-like] Listening on ws://{args.host}:{args.port} (Unity connects to /sdk/state path on same port)")
        await asyncio.sleep(args.session_secs)

    duration = int(time.time() - log.t0)
    scorecard = {
        "scenario": args.scenario,
        "persona": args.persona,
        "steps_recorded": len(log.events),
        "actions": len(log.actions_applied),
        "bugs": len(log.bug_candidates),
    }
    report = {
        "persona": args.persona,
        "scenario": args.scenario,
        "runner_mode": "editor_local",
        "duration_secs": duration,
        "events": log.events,
        "bug_candidates": log.bug_candidates,
        "scorecard": scorecard,
        "final_status": "passed",
    }
    out_path = os.path.abspath(args.report_path)
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    print(f"[human-like] Wrote report: {out_path}")

    if args.ingest:
        await post_ingest(report)

    return log


async def post_ingest(report: dict[str, Any]) -> None:
    base = os.environ.get("ARGUS_API_URL", "").rstrip("/")
    token = os.environ.get("ARGUS_TOKEN", "")
    project = os.environ.get("ARGUS_PROJECT_ID", "")
    sdk_key = os.environ.get("ARGUS_SDK_KEY", "")
    if not (base and project):
        print("[human-like] Ingest skipped: set ARGUS_API_URL and ARGUS_PROJECT_ID")
        return
    build = os.environ.get("ARGUS_BUILD_ID")
    body = {**report, "build_id": build} if build else report
    headers = {"Content-Type": "application/json"}
    if token:
        url = f"{base}/v1/projects/{project}/runs/human-like-session"
        headers["Authorization"] = f"Bearer {token}"
    elif sdk_key:
        token = await mint_sdk_session(base, project, sdk_key)
        if not token:
            print("[human-like] Ingest skipped: SDK session mint failed")
            return
        url = f"{base}/v1/projects/{project}/runs/sdk-human-like-session"
        headers["X-Argus-Key"] = token
    else:
        print("[human-like] Ingest skipped: set ARGUS_TOKEN or ARGUS_SDK_KEY")
        return
    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data,
        headers=headers,
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            response_body = resp.read().decode("utf-8")
            print("[human-like] Ingest OK:", resp.status, response_body[:500])
            try:
                payload = json.loads(response_body)
            except json.JSONDecodeError:
                return
            run_id = payload.get("id")
            dashboard = os.environ.get("ARGUS_DASHBOARD_URL", "").rstrip("/")
            if dashboard and run_id:
                run_url = f"{dashboard}/runs/{run_id}"
                print("[human-like] Dashboard run:", run_url)
                if os.environ.get("ARGUS_OPEN_DASHBOARD") == "1":
                    open_dashboard_url(run_url)
    except urllib.error.HTTPError as e:
        print("[human-like] Ingest failed:", e.code, e.read().decode("utf-8", errors="replace"))


async def mint_sdk_session(base: str, project: str, sdk_key: str) -> str | None:
    body = {"project_id": project, "sdk_key": sdk_key, "mode": "test"}
    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(
        f"{base}/sdk/session",
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
            return payload.get("token")
    except urllib.error.HTTPError as e:
        print("[human-like] SDK session mint failed:", e.code, e.read().decode("utf-8", errors="replace"))
    except Exception as exc:
        print("[human-like] SDK session mint failed:", exc)
    return None


def open_dashboard_url(url: str) -> None:
    try:
        if os.name == "nt":
            os.startfile(url)  # type: ignore[attr-defined]
        elif sys.platform == "darwin":
            subprocess.Popen(["open", url])
        else:
            subprocess.Popen(["xdg-open", url])
    except Exception as exc:
        print(f"[human-like] Could not open dashboard automatically: {exc}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Argus human-like Unity runner (WebSocket server)")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=7777)
    parser.add_argument("--session-secs", type=float, default=45.0, dest="session_secs")
    parser.add_argument("--max-steps", type=int, default=80)
    parser.add_argument(
        "--scenario",
        default="first_turn_smoke",
        choices=[
            "first_turn_smoke",
            "soak",
            "invalid_stress",
            "exploratory",
            "card_placement",
            "pawn_movement",
            "pawn_attack",
            "game_over_path",
            "ten_turn_soak",
        ],
    )
    parser.add_argument(
        "--persona",
        default="explorer",
        choices=list(PERSONA_DELAY.keys()),
    )
    parser.add_argument("--report-path", default="human_like_report.json")
    parser.add_argument("--ingest", action="store_true", help="POST report using ARGUS_* env vars")
    args = parser.parse_args()
    if args.scenario == "exploratory":
        args.scenario = "soak"
    if args.scenario == "ten_turn_soak":
        args.max_steps = max(args.max_steps, 40)
    if args.scenario == "game_over_path":
        args.session_secs = max(args.session_secs, 90.0)
    asyncio.run(main_async(args))


if __name__ == "__main__":
    main()
