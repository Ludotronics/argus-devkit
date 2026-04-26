# Argus Python SDK

Lightweight telemetry for Python game servers, headless bots, and CI pipelines.

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

## Fail-closed design

If the Argus backend is unreachable, all SDK calls silently no-op. The game server continues normally. This is intentional — never let a telemetry library crash production.

The SDK polls `/v1/sdk/config` every 60 seconds. If the backend sets `enabled: false`, all ingest calls stop immediately without a redeploy.

## Links

- [Full docs](https://argus.ludotronics.io/docs/sdk-python)
- [Portal](https://argus.ludotronics.io)
- [Unity SDK](../unity/)
- [CLI](../cli/)
