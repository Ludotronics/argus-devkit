"""
Argus Python SDK.

Supports:
  - Live mode telemetry (events + metrics) via REST ingest
  - Kill-switch / config polling
  - Fail-closed behavior: backend unreachable -> silently no-op
"""

from __future__ import annotations

import logging
import os
import queue
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
    _MAX_QUEUE_SIZE = 1000
    _BATCH_SIZE = 100

    def __init__(
        self,
        api_key: str | None = None,
        project_id: str | None = None,
        base_url: str | None = None,
        mode: str | None = None,
        timeout: float = 5.0,
        max_queue_size: int = _MAX_QUEUE_SIZE,
    ) -> None:
        self._api_key = api_key or os.getenv("ARGUS_API_KEY", "")
        self._project_id = project_id or os.getenv("ARGUS_PROJECT_ID", "")
        self._base_url = (base_url or os.getenv("ARGUS_BASE_URL", "https://api.argus.ludotronics.io")).rstrip("/")
        self._mode = (mode or os.getenv("ARGUS_MODE", "live")).lower()
        self._timeout = timeout
        self._enabled = self._mode != "off"
        self._client: httpx.Client | None = None
        self._heartbeat_thread: threading.Thread | None = None
        self._flush_thread: threading.Thread | None = None
        self._stop_event = threading.Event()
        self._queue: queue.Queue[dict[str, Any]] = queue.Queue(maxsize=max_queue_size)
        self._session_id = uuid.uuid4().hex
        self._dropped_events = 0

    # ── Lifecycle ──────────────────────────────────────────────────────────────

    def init(self) -> None:
        """Connect to Argus and start background heartbeat polling."""
        if not self._api_key or not self._project_id:
            log.warning("Argus: missing api_key or project_id; SDK disabled")
            self._enabled = False
            return
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
        self._stop_event.clear()
        self._heartbeat_thread = threading.Thread(
            target=self._heartbeat_loop, daemon=True
        )
        self._heartbeat_thread.start()
        self._flush_thread = threading.Thread(target=self._flush_loop, daemon=True)
        self._flush_thread.start()
        self.health("initialized")

    def shutdown(self) -> None:
        """Flush and close the SDK connection."""
        self.flush()
        self._stop_event.set()
        if self._client:
            self._client.close()

    # ── Telemetry ──────────────────────────────────────────────────────────────

    def event(self, name: str, properties: dict[str, Any] | None = None) -> None:
        """Emit a named gameplay event with optional properties."""
        if not self._enabled:
            return
        self._enqueue(self._build_envelope("event", {"name": name, "properties": properties or {}}))

    def metric(self, name: str, value: float, tags: dict[str, str] | None = None) -> None:
        """Emit a numeric performance or gameplay metric."""
        if not self._enabled:
            return
        self._enqueue(self._build_envelope("metric", {"name": name, "value": value, "tags": tags or {}}))

    def health(self, status: str, fields: dict[str, Any] | None = None) -> None:
        if not self._enabled:
            return
        self._enqueue(self._build_envelope("sdk_health", {
            "status": status,
            "dropped_events": self._dropped_events,
            **(fields or {}),
        }))

    def flush(self) -> None:
        batch: list[dict[str, Any]] = []
        while len(batch) < self._BATCH_SIZE:
            try:
                batch.append(self._queue.get_nowait())
            except queue.Empty:
                break
        if batch:
            self._post(batch)

    # ── Internal ───────────────────────────────────────────────────────────────

    def _enqueue(self, payload: dict[str, Any]) -> None:
        try:
            self._queue.put_nowait(payload)
        except queue.Full:
            self._dropped_events += 1

    def _post(self, payloads: list[dict[str, Any]]) -> None:
        if not self._client:
            return
        for attempt in range(3):
            try:
                response = self._client.post(self._BASE_PATH, json=payloads)
                if response.status_code in (200, 201, 202, 204):
                    return
                if response.status_code in (401, 403, 410):
                    self._enabled = False
                    return
            except Exception:
                log.debug("Argus: ingest call failed; retrying", exc_info=True)
            time.sleep(0.25 * (2 ** attempt))
        log.debug("Argus: ingest batch dropped after retries")

    def _fetch_config(self) -> None:
        if not self._client:
            return
        try:
            resp = self._client.get(self._CONFIG_PATH)
            if resp.status_code == 200:
                data = resp.json()
                self._enabled = data.get("enabled", True)
                if data.get("kill_switch") is True:
                    self._enabled = False
        except Exception:
            log.debug("Argus: config fetch failed — defaulting to enabled=True")

    def _heartbeat_loop(self) -> None:
        while not self._stop_event.wait(self._HEARTBEAT_INTERVAL):
            self._fetch_config()

    def _flush_loop(self) -> None:
        while not self._stop_event.wait(2):
            if self._enabled:
                self.flush()

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
            "privacy_mode": self._mode,
            "event_type": event_type,
            "payload": {**payload, "ts": int(time.time() * 1000)},
        }
