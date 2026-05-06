# Argus Devkit

Public developer toolkit for integrating Argus with game projects.

## How to get the devkit

- Repository: `https://github.com/ludotronics/argus-devkit`
- Clone:
  ```bash
  git clone https://github.com/ludotronics/argus-devkit.git
  ```
- Download ZIP:
  - `https://github.com/ludotronics/argus-devkit/archive/refs/heads/main.zip`

For production workflows, prefer a tagged release or pinned commit instead of floating `main`.

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

### Version pinning recommendations

- Unity (UPM git URL): pin by tag or commit SHA.
- Unreal/Godot (folder copy): track imported plugin/addon revision in your game repo.
- Python: pin package versions in `requirements.txt` or lockfiles.
