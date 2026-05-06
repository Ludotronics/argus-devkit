# Argus Unity SDK

AI-native game QA instrumentation for Unity 2022.3+.

## Get this SDK

- Source repository: `https://github.com/ludotronics/argus-devkit`
- Unity package path: `unity/`
- UPM Git URL:
  - `https://github.com/ludotronics/argus-devkit.git?path=unity#main`

For production, pin to a release tag or commit SHA instead of a floating branch.

## Quick start (5 minutes)

### 1. Install via UPM

Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.argus.sdk": "https://github.com/ludotronics/argus-devkit.git?path=unity#main"
  }
}
```

### 2. Create a config asset

**Assets > Create > Argus > Config**

Set your **API Key**, **Project ID**, backend URL, and choose a **Mode**:

```text
Backend URL: https://api.argus.ludotronics.io
```

| Mode   | When to use                                     |
|--------|-------------------------------------------------|
| `Off`  | Disabled — no overhead, no data                 |
| `Test` | Dev / CI builds — full agent instrumentation    |
| `Live` | Production / distribution — lightweight telemetry |

### 3. Add ArgusSession to your first scene

Create an empty GameObject in your first (persistent) scene,
add the **ArgusSession** component, assign your config asset.

```
[Persistent GameObject]
  └── ArgusSession  ← assign ArgusConfig here
```

That's it. `DontDestroyOnLoad` is applied automatically.

### 4. Annotate game state fields

```csharp
using Argus.SDK;

public class PlayerController : MonoBehaviour
{
    [ArgusState] public int health = 100;
    [ArgusState] public int coins = 0;
    [ArgusState("pos")] public Vector3 position => transform.position;
}
```

The Argus agent sees these fields in every snapshot — no extra code needed.

### 5. Emit custom events (optional)

```csharp
ArgusSession.Event("level_complete", new { level = 3, time = 82.4f });
ArgusSession.Purchase("gold_pack_100", 0.99m, "USD");
```

### 6. CI/CD release gate

```bash
argus push build.apk --project my-game
argus run --project my-game --persona explorer,completionist --budget 8
```

Set your pipeline to block on policy violations (for example, red scorecards or open critical bugs).

---

## Production (Live mode) setup

1. Set **Mode = Live** in your production ArgusConfig.
2. If your game requires GDPR/CCPA consent, keep **Require Consent Opt-In** enabled and call:
   ```csharp
   ArgusSession.SetConsent(true); // after user accepts
   ```
3. The post-build processor automatically adds `ARGUS_STRIP_TEST` to Release builds, ensuring Test-mode code is disabled.

---

## Resource budget (Live mode)

The SDK enforces hard limits and self-terminates if exceeded:

| Resource | Limit  |
|----------|--------|
| RAM      | 5 MB   |
| CPU      | 1 %    |
| Network  | 100 KB/session |
| Battery  | < 1 %/hr |

---

## Enterprise operations guidance

### Environment separation

- Use distinct API keys for `dev`, `staging`, and `production`.
- Keep `Test` mode in non-production builds only.
- Treat production key rotation as a scheduled runbook (recommended: 90 days).

### Privacy controls

- Enable `Require Consent Opt-In` for regions requiring explicit telemetry consent.
- Avoid sending raw PII in custom event payloads.
- Prefer stable pseudonymous IDs over direct user identifiers.

### Observability baseline

- Emit startup and session lifecycle events.
- Track key gameplay markers (`match_start`, `match_end`, `purchase`, `crash`).
- Validate telemetry throughput and dropped events during load tests.

---

## Troubleshooting

### No telemetry in Live mode

- Confirm `Mode = Live`.
- Confirm `ArgusSession.SetConsent(true)` is called after opt-in when consent is required.
- Confirm backend URL and project key are aligned.

### Runs fail after upload

- Uploaded build does not match target project or platform.
- Build did not reach `ready` state in dashboard.

### Unexpected overhead

- Reduce event cardinality and payload size.
- Move heavy custom serialization off the gameplay thread.

## Files

```
Runtime/
  Attributes/
    ArgusStateAttribute.cs   -- [ArgusState] field annotation
  Core/
    ArgusConfig.cs           -- ScriptableObject config
    ArgusSession.cs          -- Entry point MonoBehaviour
    StateStreamer.cs          -- Test: WebSocket state stream
    InputInjector.cs         -- Test: agent command receiver
    RngCapture.cs            -- Test: RNG seed capture
    KillSwitch.cs            -- Both: server-side kill switch
    SaveStateManager.cs      -- Test: checkpoint / restore
    InputShim.cs             -- Test: input system bridge
  Live/
    LiveTelemetry.cs         -- Live: crash/perf/event telemetry
Editor/
  ArgusPostBuildProcessor.cs -- Build-time Test-mode strip
```
