// Argus SDK — StateStreamer (Test mode only)
// Collects state snapshots at configurable intervals and streams them
// to the Argus backend agent over a local WebSocket.
//
// Snapshot format (JSON):
//   { "seq": 42, "ts": 1714089600.123, "scene": "Level3",
//     "objects": { "Player": { "health": 100, "pos": [1.0, 2.5, 0.0] }, ... } }

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NativeWebSocket;   // com.endel.nativewebsocket (added to UPM deps on init)
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Argus.SDK
{
    [DisallowMultipleComponent]
    public class StateStreamer : MonoBehaviour
    {
        private ArgusConfig _config;
        private WebSocket _ws;
        private int _seq;
        private bool _connected;

        public void Init(ArgusConfig config)
        {
            _config = config;
            StartCoroutine(ConnectLoop());
            StartCoroutine(SnapshotLoop());
        }

        // ---------------------------------------------------------------
        // Connection
        // ---------------------------------------------------------------

        private IEnumerator ConnectLoop()
        {
            while (true)
            {
                _ws = new WebSocket($"ws://localhost:{_config.testWsPort}/sdk/state");
                _ws.OnOpen    += () => { _connected = true; Debug.Log("[Argus] StateStreamer connected."); };
                _ws.OnClose   += _ => { _connected = false; };
                _ws.OnMessage += OnMessage;

                _ = _ws.Connect();
                yield return new WaitForSeconds(2f);
                if (!_connected)
                {
                    yield return new WaitForSeconds(3f);
                    continue;
                }
                // Keep the WebSocket pumped
                while (_connected)
                {
#if !UNITY_WEBGL || UNITY_EDITOR
                    _ws.DispatchMessageQueue();
#endif
                    yield return null;
                }
            }
        }

        // ---------------------------------------------------------------
        // Snapshot loop
        // ---------------------------------------------------------------

        private IEnumerator SnapshotLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_config.snapshotIntervalSecs);
                if (!_connected) continue;
                var snapshot = BuildSnapshot();
                _ = _ws.SendText(snapshot);
            }
        }

        private string BuildSnapshot()
        {
            var sb = new StringBuilder();
            sb.Append("{\"seq\":");
            sb.Append(_seq++);
            sb.Append(",\"ts\":");
            sb.Append(Time.unscaledTimeAsDouble.ToString("F3"));
            sb.Append(",\"state_hash\":\"");
            sb.Append(BuildStateHash());
            sb.Append("\"");
            sb.Append(",\"scene\":\"");
            sb.Append(SceneManager.GetActiveScene().name);
            sb.Append("\",\"runtime\":");
            sb.Append(ArgusSession.RuntimeMetadataJson);
            sb.Append("\",\"objects\":{");

            bool firstGo = true;
            foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            {
                var fields = mb.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var annotated = new Dictionary<string, object>();
                foreach (var fi in fields)
                {
                    var attr = fi.GetCustomAttribute<ArgusStateAttribute>();
                    if (attr == null) continue;
                    var key = attr.Key ?? fi.Name;
                    annotated[key] = fi.GetValue(mb);
                }
                if (annotated.Count == 0) continue;

                if (!firstGo) sb.Append(",");
                firstGo = false;

                sb.Append("\"");
                sb.Append(mb.gameObject.name.Replace("\"", "\\\""));
                sb.Append("\":{");
                bool firstField = true;
                foreach (var kv in annotated)
                {
                    if (!firstField) sb.Append(",");
                    firstField = false;
                    sb.Append("\""); sb.Append(kv.Key); sb.Append("\":");
                    sb.Append(ToJson(kv.Value));
                }
                sb.Append("}");
            }
            sb.Append("}}");
            return sb.ToString();
        }

        // ---------------------------------------------------------------
        // Inbound messages (agent commands — handled by InputInjector)
        // ---------------------------------------------------------------
        private void OnMessage(byte[] data)
        {
            var msg = Encoding.UTF8.GetString(data);
            InputInjector.Enqueue(msg);
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private static string ToJson(object val)
        {
            if (val == null) return "null";
            switch (val)
            {
                case bool b:   return b ? "true" : "false";
                case int i:    return i.ToString();
                case float f:  return f.ToString("F4");
                case double d: return d.ToString("F4");
                case string s: return "\"" + s.Replace("\"", "\\\"") + "\"";
                case Vector3 v:
                    return $"[{v.x:F3},{v.y:F3},{v.z:F3}]";
                case Vector2 v2:
                    return $"[{v2.x:F3},{v2.y:F3}]";
                default:
                    return JsonUtility.ToJson(val);
            }
        }

        private string BuildStateHash()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + SceneManager.GetActiveScene().name.GetHashCode();
                hash = hash * 31 + _seq;
                return hash.ToString("X");
            }
        }

        private void OnDestroy() => _ws?.Close();
    }
}
