#!/usr/bin/env python3
"""
argus CLI — game QA automation toolkit
"""
import sys
import json
import pathlib
import argparse

VERSION = "0.1.0"
CONFIG_PATH = pathlib.Path.home() / ".argus" / "config.json"


def cmd_init(args):
    """
    Auto-detect engine, generate argus.plugin.yaml, write CI workflow.
    """
    cwd = pathlib.Path.cwd()
    print(f"[argus] Scanning project at {cwd}...")

    # Engine detection heuristics
    engine = "unknown"
    if any(cwd.glob("**/*.csproj")):
        engine = "unity"
    elif any(cwd.glob("**/*.uproject")):
        engine = "unreal"
    elif any(cwd.glob("**/package.json")):
        engine = "web"
    elif any(cwd.glob("**/*.py")):
        engine = "python"

    print(f"[argus] Detected engine: {engine}")

    plugin_config = {
        "argus_version": "0.1",
        "engine": engine,
        "mode": "Test",
        "personas": ["Speedrunner", "Newbie", "Exploiter"],
        "budget": {"max_run_usd": 10, "max_duration_hours": 2},
        "invariants": [],
    }

    plugin_path = cwd / "argus.plugin.yaml"
    import yaml_shim as _  # replaced with real yaml at install time
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
    print("[argus] verify-determinism — running two-seed consistency check")
    print("[argus] Preview implementation: backend executor wiring is not enabled in this public package.")


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

    args = parser.parse_args()

    commands = {
        "init": cmd_init,
        "verify-determinism": cmd_verify_determinism,
        "replay": cmd_replay,
        "bisect": cmd_bisect,
        "run": cmd_run,
        "status": cmd_status,
        None: lambda _: parser.print_help(),
    }
    commands.get(args.command, commands[None])(args)


if __name__ == "__main__":
    main()
