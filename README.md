# Argus Devkit

Public developer toolkit for integrating Argus with game projects.

## What is included

| Component | Path | Purpose |
|---|---|---|
| Unity SDK | `unity/` | In-game instrumentation for Unity projects |
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
- Python setup: `python/README.md`
- CLI usage: `cli/README.md`

## Versioning

Devkit packages are versioned in lockstep with backend minor versions (`0.1.x` family with backend `0.1.x`).
