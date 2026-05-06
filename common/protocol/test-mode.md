# Test-mode automation vocabulary

Cross-engine capability tokens used by Argus agents and the CLI. Runtime exposure is **Test mode only**
and must be stripped from shipping builds unless explicitly enabled.

| Capability | Description |
|------------|-------------|
| `get_state` | Serialize a bounded game state vector + hash |
| `inject_input` | Apply a synthetic input action |
| `load_checkpoint` | Restore save / checkpoint |
| `save_checkpoint` | Persist save / checkpoint |
| `capture_frame` | Optional framebuffer capture (consent gated) |
| `record_trace` | Append structured trace step |
| `replay_trace` | Deterministic replay segment |
| `state_hash` | Cryptographic or rolling hash of state for determinism checks |

## CLI

`argus verify-determinism` runs two sessions with the same seed, collects ordered `state_hash`
values, and reports the first diverging frame/tick when hashes do not match.

## Mode gates

Commands that mutate input or load automation checkpoints **must** return a structured
“unsupported” result when `privacy_mode` / build profile disallows Test automation.
