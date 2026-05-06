#!/usr/bin/env python3
"""
argus CLI — game QA automation toolkit
"""
import sys
import json
import pathlib
import argparse
import urllib.request
import urllib.parse

VERSION = "0.1.0"
CONFIG_PATH = pathlib.Path.home() / ".argus" / "config.json"


def _detect_engine(cwd: pathlib.Path) -> str:
    # Prioritise explicit project markers over generic source files.
    if (cwd / "project.godot").exists():
        return "godot"
    if any(cwd.glob("**/*.uproject")):
        return "unreal"
    if (cwd / "ProjectSettings" / "ProjectVersion.txt").exists() or any(cwd.glob("**/*.asmdef")):
        return "unity"
    if any(cwd.glob("**/*.py")):
        return "python"
    if any(cwd.glob("**/package.json")):
        return "web"
    return "unknown"


def cmd_init(args):
    """
    Auto-detect engine, generate argus.plugin.yaml, write CI workflow.
    """
    cwd = pathlib.Path.cwd()
    print(f"[argus] Scanning project at {cwd}...")

    engine = _detect_engine(cwd)

    print(f"[argus] Detected engine: {engine}")

    plugin_config = {
        "argus_manifest_version": "1.0.0",
        "argus_version": "0.1",
        "engine": engine,
        "mode": "Test",
        "capabilities": {
            "live_telemetry": True,
            "killswitch": True,
            "flags": True,
            "state_capture": engine in {"unity", "unreal", "godot"},
            "input_injection": engine in {"unity", "unreal", "godot"},
            "save_load_hooks": engine in {"unity", "unreal", "godot"},
            "screenshot": True,
            "replay": engine in {"unity", "unreal", "godot"},
            "privacy_redaction": True,
        },
        "personas": ["Speedrunner", "Newbie", "Exploiter"],
        "budget": {"max_run_usd": 10, "max_duration_hours": 2},
        "invariants": [],
    }

    plugin_path = cwd / "argus.plugin.yaml"
    with open(plugin_path, "w") as f:
        for k, v in plugin_config.items():
            f.write(f"{k}: {json.dumps(v)}\n")

    print(f"[argus] Created {plugin_path}")
    print("[argus] Next steps:")
    print("  1. Add the Argus SDK to your game project")
    print("  2. Run: argus verify-determinism")
    print("  3. Push a build: argus upload <path>")


def cmd_verify_determinism(args):
    """
    Run the same seed twice and compare state hashes.
    Requires the Argus SDK in Test mode running on an emulator.
    """
    print("[argus] verify-determinism — run same seed twice and compare state_hash outputs")
    print("[argus] Expected flow: collect two state hash streams -> diff by frame/tick -> report first mismatch.")


def cmd_replay(args):
    """
    Deterministically replay a bug by ID.
    argus replay BUG-112
    """
    bug_id = args.bug_id
    print(f"[argus] Replaying {bug_id}...")
    print("[argus] Preview implementation: replay transport is not enabled in this public package.")


def cmd_bisect(args):
    """
    Auto-bisect commits to find the introducing commit for a bug.
    argus bisect BUG-112
    """
    bug_id = args.bug_id
    print(f"[argus] Bisecting {bug_id} across commits...")
    print("[argus] Preview implementation: bisect transport is not enabled in this public package.")


def cmd_run(args):
    """
    Start an agent run.
    argus run --persona newbie --duration 10m --build latest
    """
    print(f"[argus] Starting run: persona={args.persona} duration={args.duration}")
    print("[argus] Preview implementation: run transport is not enabled in this public package.")


def cmd_status(args):
    print(f"argus CLI v{VERSION}")
    print("Backend: not configured — run 'argus init' first")


def cmd_verify_manifest(args):
    manifest_path = pathlib.Path(args.path)
    if not manifest_path.exists():
        print(f"[argus] Manifest not found: {manifest_path}")
        sys.exit(1)

    required = {"argus_manifest_version", "engine", "mode"}
    parsed = {}
    for line in manifest_path.read_text().splitlines():
        if ":" not in line:
            continue
        k, v = line.split(":", 1)
        parsed[k.strip()] = v.strip()
    missing = sorted(required - parsed.keys())
    if missing:
        print(f"[argus] Manifest invalid. Missing keys: {', '.join(missing)}")
        sys.exit(1)
    print(f"[argus] Manifest valid ({manifest_path})")


def cmd_send_heartbeat(args):
    backend = args.backend.rstrip("/")
    project_id = args.project_id
    sdk_key = args.api_key
    url = f"{backend}/sdk/live/ingest"
    payload = [{
        "schema_version": "1.0.0",
        "sdk_name": "argus-cli",
        "sdk_version": VERSION,
        "engine": "python",
        "engine_version": "cli",
        "platform": "server",
        "project_id": project_id,
        "session_id": "heartbeat-cli",
        "privacy_mode": "live",
        "event_type": "sdk_health",
        "payload": {"source": "argus-cli", "heartbeat": True},
    }]
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data,
        method="POST",
        headers={
            "Content-Type": "application/json",
            "X-Argus-Key": sdk_key,
            "X-Argus-Project": project_id,
        },
    )
    with urllib.request.urlopen(req, timeout=10) as response:
        body = response.read().decode("utf-8")
    print(f"[argus] Heartbeat sent to {url}")
    print(body)


def cmd_doctor(args):
    cwd = pathlib.Path.cwd()
    print("[argus] Doctor checks:")
    checks = [
        ("argus.plugin.yaml exists", (cwd / "argus.plugin.yaml").exists()),
        ("engine detected", _detect_engine(cwd) != "unknown"),
    ]
    for label, ok in checks:
        print(f"  [{'ok' if ok else '!!'}] {label}")
    if args.backend and args.project_id and args.api_key:
        try:
            query = urllib.parse.urlencode({"project_id": args.project_id, "sdk_name": "argus-cli", "sdk_version": VERSION})
            with urllib.request.urlopen(f"{args.backend.rstrip('/')}/sdk/config?{query}", timeout=10) as response:
                print(f"  [ok] backend config endpoint ({response.status})")
            health_req = urllib.request.Request(
                f"{args.backend.rstrip('/')}/sdk/health/{args.project_id}",
                headers={"X-Argus-Key": args.api_key},
                method="GET",
            )
            with urllib.request.urlopen(health_req, timeout=10) as response:
                print(f"  [ok] sdk health auth ({response.status})")
        except Exception as exc:
            print(f"  [!!] backend config endpoint failed: {exc}")
            sys.exit(1)
    print("[argus] Doctor completed.")


def main():
    parser = argparse.ArgumentParser(prog="argus", description="Argus game QA CLI")
    parser.add_argument("--version", action="version", version=f"%(prog)s {VERSION}")
    sub = parser.add_subparsers(dest="command")

    sub.add_parser("init", help="Initialise Argus in the current project")

    p_det = sub.add_parser("verify-determinism", help="Run determinism preflight check")

    p_rep = sub.add_parser("replay", help="Replay a bug deterministically")
    p_rep.add_argument("bug_id", help="Bug ID, e.g. BUG-112")

    p_bis = sub.add_parser("bisect", help="Auto-bisect commits to find introducing PR")
    p_bis.add_argument("bug_id")

    p_run = sub.add_parser("run", help="Start an agent run")
    p_run.add_argument("--persona", default="Speedrunner")
    p_run.add_argument("--duration", default="30m")
    p_run.add_argument("--build", default="latest")

    sub.add_parser("status", help="Show CLI configuration status")

    p_manifest = sub.add_parser("verify-manifest", help="Validate argus.plugin.yaml has required keys")
    p_manifest.add_argument("--path", default="argus.plugin.yaml")

    p_heartbeat = sub.add_parser("send-heartbeat", help="Send a schema-valid SDK health event")
    p_heartbeat.add_argument("--backend", required=True)
    p_heartbeat.add_argument("--project-id", required=True)
    p_heartbeat.add_argument("--api-key", required=True)

    p_doctor = sub.add_parser("doctor", help="Run local and backend integration checks")
    p_doctor.add_argument("--backend")
    p_doctor.add_argument("--project-id")
    p_doctor.add_argument("--api-key")

    args = parser.parse_args()

    commands = {
        "init": cmd_init,
        "verify-determinism": cmd_verify_determinism,
        "replay": cmd_replay,
        "bisect": cmd_bisect,
        "run": cmd_run,
        "status": cmd_status,
        "verify-manifest": cmd_verify_manifest,
        "send-heartbeat": cmd_send_heartbeat,
        "doctor": cmd_doctor,
        None: lambda _: parser.print_help(),
    }
    commands.get(args.command, commands[None])(args)


if __name__ == "__main__":
    main()
