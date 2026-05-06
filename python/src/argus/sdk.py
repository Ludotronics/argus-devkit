"""
Argus Python SDK.

Supports:
  - Live mode telemetry (events + metrics) via REST ingest
  - Kill-switch / config polling
  - Fail-closed behavior: backend unreachable -> silently no-op
"""

from __future__ import annotations

import logging
import threading
import time
import uuid
from typing import Any

import httpx

log = logging.getLogger("argus")


class Argus:
    """Main SDK entry point.

    Usage::

        from argus import Argus

        sdk = Argus(api_key="ak_live_YOUR_KEY", project_id="my-game")
        sdk.init()

        sdk.event("quest_completed", {"quest_id": "q-14", "duration_s": 182})
        sdk.metric("fps_avg", 57.3)
    """

    _BASE_PATH = "/sdk/live/ingest"
    _CONFIG_PATH = "/sdk/config"
    _HEARTBEAT_INTERVAL = 60  # seconds

    def __init__(
        self,
        api_key: str,
        project_id: str,
        base_url: str = "https://api.argus.ludotronics.io",
        timeout: float = 5.0,
    ) -> None:
        self._api_key = api_key
        self._project_id = project_id
        self._base_url = base_url.rstrip("/")
        self._timeout = timeout
        self._enabled = True
        self._client: httpx.Client | None = None
        self._heartbeat_thread: threading.Thread | None = None
        self._session_id = uuid.uuid4().hex

    # ── Lifecycle ──────────────────────────────────────────────────────────────

    def init(self) -> None:
        """Connect to Argus and start background heartbeat polling."""
        self._client = httpx.Client(
            base_url=self._base_url,
            headers={
                "X-Argus-Key": self._api_key,
                "X-Argus-Project": self._project_id,
                "Content-Type": "application/json",
            },
            timeout=self._timeout,
        )
        self._fetch_config()
        self._heartbeat_thread = threading.Thread(
            target=self._heartbeat_loop, daemon=True
        )
        self._heartbeat_thread.start()

    def shutdown(self) -> None:
        """Flush and close the SDK connection."""
        self._enabled = False
        if self._client:
            self._client.close()

    # ── Telemetry ──────────────────────────────────────────────────────────────

    def event(self, name: str, properties: dict[str, Any] | None = None) -> None:
        """Emit a named gameplay event with optional properties."""
        if not self._enabled:
            return
        self._post(self._build_envelope("event", {"name": name, "properties": properties or {}}))

    def metric(self, name: str, value: float, tags: dict[str, str] | None = None) -> None:
        """Emit a numeric performance or gameplay metric."""
        if not self._enabled:
            return
        self._post(self._build_envelope("metric", {"name": name, "value": value, "tags": tags or {}}))

    # ── Internal ───────────────────────────────────────────────────────────────

    def _post(self, payload: dict[str, Any]) -> None:
        if not self._client:
            return
        try:
            self._client.post(self._BASE_PATH, json=[payload])
        except Exception:
            log.debug("Argus: ingest call failed — SDK is fail-closed, continuing silently")

    def _fetch_config(self) -> None:
        if not self._client:
            return
        try:
            resp = self._client.get(self._CONFIG_PATH)
            if resp.status_code == 200:
                data = resp.json()
                self._enabled = data.get("enabled", True)
        except Exception:
            log.debug("Argus: config fetch failed — defaulting to enabled=True")

    def _heartbeat_loop(self) -> None:
        while self._enabled:
            time.sleep(self._HEARTBEAT_INTERVAL)
            self._fetch_config()

    def _build_envelope(self, event_type: str, payload: dict[str, Any]) -> dict[str, Any]:
        return {
            "schema_version": "1.0.0",
            "sdk_name": "argus-python",
            "sdk_version": "0.1.0",
            "engine": "python",
            "engine_version": "3",
            "platform": "server",
            "project_id": self._project_id,
            "session_id": self._session_id,
            "privacy_mode": "live",
            "event_type": event_type,
            "payload": {**payload, "ts": int(time.time() * 1000)},
        }
