# Argus Godot SDK

Enterprise integration guide for Godot teams using Argus for test automation and live telemetry.

## Compatibility

- Godot: 4.2+
- Targets: desktop and server-authoritative workflows
- Modes:
  - `Test`: QA automation-focused instrumentation
  - `Live`: lightweight production telemetry
  - `Off`: disabled

## Prerequisites

- Argus API key and project ID
- Backend URL (`https://api.argus.ludotronics.io`)
- Plugin-enabled Godot project with `project.godot`
- CI pipeline that exports distributable artifacts

## Get this SDK

- Source repository: `https://github.com/ludotronics/argus-devkit`
- Godot path in repository: `godot/`
- Download ZIP: `https://github.com/ludotronics/argus-devkit/archive/refs/heads/main.zip`

For production, pin to a release tag or commit and track imported addon revision in your game repository.

## Install

1. Copy addon files into your project:

```bash
cp -R argus-devkit/godot/addons/argus /path/to/game/addons/argus
```

2. Enable plugin in `Project > Project Settings > Plugins`.
3. Confirm `Argus` autoload or bootstrap script initialization in startup scene.

## Configure

Define a config resource or environment settings with:

- `api_key`
- `project_id`
- `backend_url`
- `mode`
- `consent_required`

Recommended environment setup:

- Local dev and CI: `Test`
- Staging: `Test` or constrained `Live`
- Production: `Live`

## First Event Validation

```gdscript
extends Node

func _ready() -> void:
    Argus.init("ak_live_YOUR_KEY", "my-game", "https://api.argus.ludotronics.io", "test")
    Argus.event("startup_ready", {"scene": get_tree().current_scene.name})
```

Validate in portal:

- Event flow visible in `/live`
- Build accepted and runnable
- Test runs visible in `/runs`

## CI/CD Integration

Export build and trigger Argus validation gate:

```bash
argus push build.zip --project my-game
argus run --project my-game --persona explorer,completionist --budget 6
```

Set pipeline failure on red scorecard or policy violation.

## Performance and Runtime Guidance

- Emit coarse-grained events, not frame-level spam.
- Batch metrics where possible.
- Keep serialization off hot gameplay paths.
- Ensure graceful flush at process exit.

## Security and Privacy

- Avoid player identifiers unless hashed/tokenized.
- Respect user telemetry consent in regulated regions.
- Rotate keys and scope by environment.
- Keep keys out of source control.

## Troubleshooting

### Plugin enabled but no telemetry

- Confirm startup bootstrap executes.
- Confirm mode is not `off`.
- Validate backend URL and TLS reachability.

### Unauthorized responses

- Invalid API key or wrong project binding.
- Key rotated but runtime still using stale value.

### CI runs fail after upload

- Build package invalid for configured platform.
- Project mismatch between upload and run command.

## Included structure

- `addons/argus/plugin.cfg`
- `addons/argus/plugin.gd`
- `addons/argus/runtime.gd`
- `samples/` and `tests/` scaffolds for integration validation

## Related

- Unity guide: `../unity/README.md`
- Unreal guide: `../unreal/README.md`
- Python guide: `../python/README.md`
- Protocol docs: `../common/protocol/README.md`
