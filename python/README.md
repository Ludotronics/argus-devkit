# Argus Python SDK

Lightweight telemetry for Python game servers, headless bots, and CI pipelines.

## Get this SDK

- Package install: `pip install argus-sdk`
- Source repository: `https://github.com/ludotronics/argus-devkit`
- Python SDK path in repo: `python/`

For production, pin package versions in lockfiles and align with backend minor version compatibility.

## Compatibility

- Python 3.10+
- Linux/macOS/Windows
- Works in API services, worker queues, simulation bots, and CI tasks

## Install

```bash
pip install argus-sdk
```

## Quick start

```python
from argus import Argus

sdk = Argus(api_key="ak_live_YOUR_KEY", project_id="my-game")
sdk.init()

# Emit a gameplay event
sdk.event("quest_completed", {"quest_id": "q-14", "duration_s": 182})

# Emit a numeric metric
sdk.metric("fps_avg", 57.3)

# Clean shutdown (optional — daemon thread exits on process end)
sdk.shutdown()
```

## Configuration model

Recommended runtime configuration:

- `api_key`: environment-scoped key
- `project_id`: target project slug/id
- `base_url`: backend endpoint
- `mode`: `test`, `live`, `off`
- `flush_interval`: batching cadence tuned to workload

Use environment variables for production:

```bash
ARGUS_API_KEY=ak_live_prod_...
ARGUS_PROJECT_ID=my-game
ARGUS_BASE_URL=https://api.argus.ludotronics.io
ARGUS_MODE=live
```

## Fail-closed design

If the Argus backend is unreachable, all SDK calls silently no-op. The game server continues normally. This is intentional — never let a telemetry library crash production.

The SDK polls `/v1/sdk/config` every 60 seconds. If the backend sets `enabled: false`, all ingest calls stop immediately without a redeploy.

## CI/CD usage pattern

Use Python SDK for synthetic workload and backend signal generation, then gate release with CLI:

```bash
argus push build.zip --project my-game
argus run --project my-game --persona explorer,destructor --budget 8
```

Use scorecard outcome as deployment control.

## Security and privacy

- Never log or emit raw secrets in event payloads.
- Avoid direct PII; hash/tokenize identities before send.
- Rotate API keys regularly and isolate by environment.
- Restrict outbound traffic to trusted backend domains.

## Troubleshooting

### No events in portal

- Check `ARGUS_MODE` and project ID.
- Verify process has network egress.
- Ensure flush interval is not too long for short-lived jobs.

### 401/403 responses

- Key revoked or scoped to a different project.
- Old key cached in container/task definition.

### High latency or dropped batches

- Reduce event batch size.
- Increase retry and backoff limits.
- Ensure worker shutdown calls `sdk.shutdown()`.

## Links

- [Full docs](https://argus.ludotronics.io/docs/sdk-python)
- [Portal](https://argus.ludotronics.io)
- [Unity SDK](../unity/)
- [CLI](../cli/)
