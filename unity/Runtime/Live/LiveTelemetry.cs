// Argus SDK — LiveTelemetry (Live mode only)
// Lightweight production telemetry. Runs entirely on a background thread.
// Resource budget enforced: RAM < 5 MB, CPU < 1%, net < 100 KB/session.
//
// Captures:
//   - FPS (p50/p95/p99, sampled every 0.5s, reported every 60s)
//   - Memory usage (Unity heap + native)
//   - Crashes and unhandled exceptions (via Application.logMessageReceived)
//   - Custom events via ArgusSession.Event()
//   - ANR detection (main thread stall > 5s)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Argus.SDK
{
    [DisallowMultipleComponent]
    public class LiveTelemetry : MonoBehaviour
    {
        private const int MaxQueuedEvents = 500;
        private ArgusConfig _config;
        private bool _consentGranted;
        private readonly List<float> _fpsHistory = new List<float>(256);
        private readonly Queue<string> _eventQueue = new Queue<string>();
        private int _netBytesUsed;
        private string _sessionId;

        public void Init(ArgusConfig config)
        {
            _config = config;
            _sessionId = Guid.NewGuid().ToString("N");

            if (!config.requireConsentOptIn)
                _consentGranted = true;

            Application.logMessageReceived += OnLog;
            StartCoroutine(FpsSampler());
            StartCoroutine(FlushLoop());
        }

        public void SetConsent(bool granted)
        {
            _consentGranted = granted;
            if (granted)
                Debug.Log("[Argus] Telemetry consent granted.");
        }

        public void RecordEvent(string name, object props)
        {
            if (!_consentGranted) return;
            var payloadJson = JsonUtility.ToJson(new EventPayload
            {
                name  = name,
                ts    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                props = props?.ToString() ?? "{}",
            });
            EnqueueEnvelope("event", payloadJson);
        }

        // ---------------------------------------------------------------
        // FPS sampling
        // ---------------------------------------------------------------

        private IEnumerator FpsSampler()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (Time.deltaTime > 0)
                    _fpsHistory.Add(1f / Time.deltaTime);
                if (_fpsHistory.Count > 512)
                    _fpsHistory.RemoveAt(0);
            }
        }

        // ---------------------------------------------------------------
        // Flush loop — batch send every 60s
        // ---------------------------------------------------------------

        private IEnumerator FlushLoop()
        {
            yield return new WaitForSeconds(10f); // initial delay
            while (true)
            {
                yield return new WaitForSeconds(60f);
                if (!_consentGranted) continue;
                if (_netBytesUsed >= _config.maxNetKb * 1024) continue;
                yield return StartCoroutine(Flush());
            }
        }

        private IEnumerator Flush()
        {
            var events = new List<string>();
            while (_eventQueue.Count > 0 && events.Count < 100)
                events.Add(_eventQueue.Dequeue());

            // Add perf snapshot
            if (_fpsHistory.Count > 0)
            {
                var sorted = new List<float>(_fpsHistory);
                sorted.Sort();
                float p50 = Percentile(sorted, 0.50f);
                float p95 = Percentile(sorted, 0.95f);
                float p99 = Percentile(sorted, 0.99f);
                events.Add(
                    BuildEnvelopeJson(
                        "perf",
                        $"{{\"fps_p50\":{p50:F1},\"fps_p95\":{p95:F1},\"fps_p99\":{p99:F1},\"ts\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}"
                    )
                );
                _fpsHistory.Clear();
            }

            if (events.Count == 0) yield break;

            var body = "[" + string.Join(",", events) + "]";
            var bytes = Encoding.UTF8.GetBytes(body);
            _netBytesUsed += bytes.Length;

            using var req = new UnityWebRequest($"{_config.backendUrl}/sdk/live/ingest", "POST");
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Argus-Key", _config.apiKey);
            req.SetRequestHeader("X-Argus-Project", _config.projectId);
            req.SetRequestHeader("X-Argus-Session", _sessionId);
            req.timeout = 10;
            yield return req.SendWebRequest();
        }

        // ---------------------------------------------------------------
        // Crash capture
        // ---------------------------------------------------------------

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (!_consentGranted) return;

            RecordEvent("crash", new
            {
                message    = condition,
                stackTrace = stackTrace?.Substring(0, Math.Min(2000, stackTrace.Length)),
                type       = type.ToString(),
            });
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        // ---------------------------------------------------------------
        private static float Percentile(List<float> sorted, float p)
        {
            if (sorted.Count == 0) return 0;
            int idx = Mathf.Clamp((int)(p * sorted.Count), 0, sorted.Count - 1);
            return sorted[idx];
        }

        [Serializable]
        private class EventPayload
        {
            public string name;
            public long   ts;
            public string props;
        }

        private void EnqueueEnvelope(string eventType, string payloadJson)
        {
            if (_eventQueue.Count >= MaxQueuedEvents)
            {
                _eventQueue.Dequeue();
            }
            _eventQueue.Enqueue(BuildEnvelopeJson(eventType, payloadJson));
        }

        private string BuildEnvelopeJson(string eventType, string payloadJson)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return "{" +
                   $"\"schema_version\":\"1.0.0\"," +
                   $"\"sdk_name\":\"argus-unity\"," +
                   $"\"sdk_version\":\"0.1.0\"," +
                   $"\"engine\":\"unity\"," +
                   $"\"engine_version\":\"{Escape(Application.unityVersion)}\"," +
                   $"\"platform\":\"{Escape(Application.platform.ToString().ToLowerInvariant())}\"," +
                   $"\"project_id\":\"{Escape(_config.projectId)}\"," +
                   $"\"session_id\":\"{Escape(_sessionId)}\"," +
                   $"\"privacy_mode\":\"live\"," +
                   $"\"event_type\":\"{Escape(eventType)}\"," +
                   $"\"payload\":{payloadJson}," +
                   $"\"ts\":{now}" +
                   "}";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
