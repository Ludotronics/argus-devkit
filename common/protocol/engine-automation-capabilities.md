# Engine integration tiers and automation capabilities

Argus uses one wire protocol with three integration tiers.

| Tier | Purpose | Production expectation |
|---|---|---|
| `protocol_only` | Direct HTTP ingest clients | No engine editor integration |
| `native_live` | Engine-native production telemetry | Runtime-safe queue, flags, kill switch |
| `native_test_automation` | Autonomous QA controls | State/input/replay hooks behind test mode |

Current capability map by engine:

| Engine | Tier focus | Capability surface | Status |
|---|---|---|---|
| Unity | `native_live` + `native_test_automation` | `IAutomationBridge`, `StateStreamer`, `InputInjector` | in progress |
| Unreal | `native_live` + `native_test_automation` | `ArgusRuntime` + `ArgusDeveloper` modules | in progress |
| Godot | `native_live` + `native_test_automation` | addon runtime + editor plugin scaffolds | in progress |
| Python | `protocol_only` | direct ingest wrappers | beta |
