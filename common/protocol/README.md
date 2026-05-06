# Argus SDK wire protocol

Engine-neutral JSON contracts for `/sdk/live/ingest`, `/sdk/killswitch`, and `/sdk/flags`.
All live telemetry events share one **versioned envelope** so Unity, Unreal, Godot, and server SDKs stay aligned.

## Versions

| Schema | Status | Notes |
|--------|--------|-------|
| `1.0.0` | Current | Required for new integrations |

Clients **must** send `schema_version: "1.0.0"` until a newer schema is published. The backend rejects unknown schema versions with HTTP `422`.

## Event envelope (required fields)

Every item POSTed to `/sdk/live/ingest` (batch JSON array) must be an object:

| Field | Type | Description |
|-------|------|-------------|
| `schema_version` | string | e.g. `"1.0.0"` |
| `sdk_name` | string | Package id, e.g. `argus-unity`, `argus-unreal`, `argus-godot` |
| `sdk_version` | string | Semver of the SDK package |
| `engine` | string | `unity` \| `unreal` \| `godot` \| `web` \| `python` |
| `engine_version` | string | Engine/editor version string |
| `platform` | string | Runtime platform (e.g. `android`, `windows`, `linux`, `macos`, `ios`, `webgl`, `server`) |
| `project_id` | string | Argus project id (must match dashboard / `X-Argus-Project` when set) |
| `session_id` | string | Per-run session id |
| `privacy_mode` | string | `live`, `test`, `off`, or `anonymous` |
| `event_type` | string | One of the event classes below |
| `payload` | object | Type-specific payload (bounded size server-side) |

### Headers

| Header | Required | Purpose |
|--------|----------|---------|
| `X-Argus-Key` | Yes | Long-lived SDK key **or** short-lived session token |
| `X-Argus-Project` | Recommended | Overrides default project resolution from token |
| `X-Argus-Session` | Optional | Transport hint; should mirror `session_id` when possible |

### Event classes (`event_type`)

| `event_type` | Purpose |
|--------------|---------|
| `event` | Named product/analytics event |
| `metric` | Gauge/counter style measurement |
| `perf` | Frame time, FPS, memory samples |
| `crash` | Fatal error / native crash summary |
| `breadcrumb` | Lightweight trail |
| `state_snapshot` | Test-mode state vector (optional in Live) |
| `replay_marker` | Determinism / replay fence |
| `sdk_health` | Connectivity and queue health |

### Limits (server-enforced)

- Max request body size: **512 KiB**
- Max events per request: **200**
- Max JSON size per event object: **64 KiB**
- Max `payload` serialized size: **32 KiB**

### Compatibility rules

1. **Additive changes only** within the same `schema_version` (new optional envelope fields, new `payload` keys).
2. **Breaking changes** require a new `schema_version` and a published migration window.
3. Engines **must not** send PII in `payload` unless `privacy_mode` and product consent allow it; use redaction hooks client-side.

## JSON Schemas

Machine-readable schemas live in `schemas/`:

- `event-envelope.schema.json` — single ingest item
- `event-payloads.schema.json` — optional constraints per `event_type`

## Fixtures

Golden examples for CI and docs: `../test-fixtures/*.json`.
