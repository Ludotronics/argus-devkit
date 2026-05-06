# Argus Unreal SDK

Enterprise integration guide for Unreal Engine teams using Argus for autonomous QA and production telemetry.

## Compatibility

- Unreal Engine: 5.3+ (validated target baseline)
- Platforms: Win64, Linux (editor and dedicated server workflows)
- Modes:
  - `Test`: deep automation and validation instrumentation
  - `Live`: low-overhead production telemetry
  - `Off`: disabled

## Prerequisites

- Argus org/project with environment-specific API keys
- Backend URL (`https://api.argus.ludotronics.io`)
- CI pipeline capable of packaging and publishing Unreal builds
- Security review for telemetry fields and consent workflow

## Get this SDK

- Source repository: `https://github.com/ludotronics/argus-devkit`
- Unreal path in repository: `unreal/`
- Download ZIP: `https://github.com/ludotronics/argus-devkit/archive/refs/heads/main.zip`

For production, pin to a release tag or specific commit and record that revision in your game repo.

## Install

1. Copy this folder to your project plugin directory:

```bash
cp -R argus-devkit/unreal /path/to/MyGame/Plugins/Argus
```

2. Enable plugin in `Edit > Plugins`.
3. Restart the editor and ensure modules load:
   - `ArgusRuntime`
   - `ArgusEditor`
   - `ArgusDeveloper`

## Configure (per environment)

Create environment-specific config values in project settings:

- `Argus.ApiKey`
- `Argus.ProjectId`
- `Argus.BackendUrl`
- `Argus.Mode` (`Test`, `Live`, `Off`)
- `Argus.RequireConsent` (`true` for production where required)

Recommended environment mapping:

- Dev/CI: `Test`
- Staging: `Test` or `Live` (limited)
- Production: `Live`

## First Event Validation

After startup initialization, emit a smoke event from game bootstrap:

```cpp
// Pseudocode - adapt to your runtime wrapper
Argus::Init(ApiKey, ProjectId, BackendUrl, EArgusMode::Test);
Argus::Event("startup_ready", {{"build", BuildVersion}, {"map", MapName}});
```

Validate in portal:

- `/live` shows event traffic
- `/runs` accepts run execution
- `/bugs` receives issues from automated sessions

## CI/CD Integration

Standard release flow:

1. Package build in CI.
2. Upload via CLI:

```bash
argus push MyGame-Win64.zip --project my-game
```

3. Trigger run matrix:

```bash
argus run --project my-game --persona explorer,destructor --budget 8
```

4. Enforce scorecard gate before promotion.

## Performance Budget (Live Mode)

Target operational limits:

- CPU overhead: <1%
- Memory overhead: <8 MB
- Network burst: bounded and batched
- No gameplay thread stalls caused by telemetry

Use buffered async transport and periodic flush intervals.

## Security and Privacy

- Do not emit raw PII in events.
- Use consent gate before telemetry in privacy-sensitive regions.
- Rotate API keys on schedule (90 days recommended).
- Separate keys by environment and game project.

## Troubleshooting

### Plugin loads but no events

- Verify module load in logs.
- Confirm `Argus.Mode` is not `Off`.
- Validate backend reachability from host.

### 401/403 from ingest

- API key mismatch or revoked key.
- Wrong project ID or environment key used.

### Events delayed or dropped

- Batch size too high or flush interval too aggressive.
- Retry policy disabled or process exits before final flush.

## Directory overview

- `Source/ArgusRuntime`: runtime transport and mode logic
- `Source/ArgusEditor`: setup and validation helpers
- `Source/ArgusDeveloper`: test automation hooks
- `Argus.uplugin`: plugin metadata and modules

## Related

- Unity guide: `../unity/README.md`
- Godot guide: `../godot/README.md`
- Python guide: `../python/README.md`
- Wire protocol: `../common/protocol/README.md`
