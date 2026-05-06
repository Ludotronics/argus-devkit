# Argus Devkit

Public developer toolkit for integrating Argus with game projects.

## What is included

| Component | Path | Purpose |
|---|---|---|
| Unity SDK | `unity/` | In-game instrumentation for Unity projects |
| Unreal SDK (MVP) | `unreal/` | Plugin skeleton for Unreal runtime/editor integration |
| Godot addon (MVP) | `godot/` | Addon skeleton for Godot editor/runtime integration |
| Common protocol | `common/protocol/` | Engine-neutral ingest envelope and payload schemas |
| Python SDK | `python/` | Telemetry client for game servers, bots, and automation |
| CLI | `cli/` | Local command-line tooling for setup and run orchestration |

## Modes

Argus integrations use three runtime modes:

| Mode | Use case |
|---|---|
| `Test` | Development and CI validation |
| `Live` | Production-safe telemetry |
| `Off` | Disabled |

## Quick links

- Unity setup: `unity/README.md`
- Unreal setup: `unreal/README.md`
- Godot setup: `godot/README.md`
- Python setup: `python/README.md`
- Shared protocol: `common/protocol/README.md`
- Integration tiers: `common/protocol/engine-automation-capabilities.md`
- CLI usage: `cli/README.md`

## Versioning

Devkit packages are versioned in lockstep with backend minor versions (`0.1.x` family with backend `0.1.x`).
