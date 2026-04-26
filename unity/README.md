# Argus Unity SDK

AI-native game QA instrumentation for Unity 2022.3+.

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

Set your **API Key** and choose a **Mode**:

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
