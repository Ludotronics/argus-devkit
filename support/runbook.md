# Argus Devkit Support Runbook

## Intake

1. Capture engine (`unity` / `unreal` / `godot` / `python`) and SDK version.
2. Request `argus.plugin.yaml` and failing endpoint/status code.
3. Confirm privacy mode and environment (test/live).

## First-response checks

- Validate `schema_version` is `1.0.0`.
- Ensure `X-Argus-Key` is present and not revoked.
- Check payload count does not exceed 200 events/request.
- Confirm request body is under 512 KiB.
- Run `argus doctor --backend <url> --project-id <id> --api-key <token>`.
- Run `argus verify-manifest --path argus.plugin.yaml`.

## Structured error codes

| Code | Meaning | Typical fix |
|---|---|---|
| `invalid_key` | Missing/invalid SDK token | Rotate key and verify header |
| `revoked_key` | Token explicitly revoked | Mint a new session token |
| `rate_limited` | Request burst exceeded limit | Backoff/jitter and batch |
| `schema_mismatch` | Unsupported `schema_version` | Upgrade SDK or pin supported version |
| `invalid_engine` | Engine field invalid | Use one of `unity/unreal/godot/web/python` |
| `invalid_event_type` | Event class unsupported | Emit a supported event type |
| `payload_too_large` | Event or payload too big | Trim payload, split event |
| `request_too_large` | Batch body > 512 KiB | Send smaller batches |
| `too_many_events` | Batch > 200 events | Reduce per-request event count |

## Escalation bundle

- Minimal failing event envelope JSON
- Engine/editor version
- SDK logs with timestamp window
