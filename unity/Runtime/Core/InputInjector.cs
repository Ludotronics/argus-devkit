// Argus SDK — InputInjector (Test mode only)
// Receives agent action commands from the StateStreamer WebSocket
// and injects them as synthetic Unity input events.
//
// Supported actions (JSON):
//   {"action":"tap",   "x":0.5,"y":0.3}          -- normalised screen coords
//   {"action":"swipe", "x0":0.5,"y0":0.8,"x1":0.5,"y1":0.2,"duration":0.3}
//   {"action":"key",   "key":"Escape"}
//   {"action":"axis",  "axis":"Horizontal","value":1.0}
//   {"action":"wait",  "seconds":0.5}
//   {"action":"save_state"}
//   {"action":"load_state","slot":0}

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Argus.SDK
{
    [DisallowMultipleComponent]
    public class InputInjector : MonoBehaviour
    {
        private static readonly Queue<string> _queue = new Queue<string>();
        private ArgusConfig _config;

        public void Init(ArgusConfig config) => _config = config;

        /// <summary>Called by StateStreamer on every inbound WebSocket message.</summary>
        public static void Enqueue(string json) => _queue.Enqueue(json);

        private void Update()
        {
            while (_queue.Count > 0)
                Process(_queue.Dequeue());
        }

        private static void Process(string json)
        {
            try
            {
                var cmd = JsonUtility.FromJson<ActionCommand>(json);
                switch (cmd.action)
                {
                    case "tap":
                        SimulateTap(cmd.x, cmd.y);
                        break;
                    case "key":
                        SimulateKey(cmd.key);
                        break;
                    case "save_state":
                        SaveStateManager.SaveSlot(0);
                        break;
                    case "load_state":
                        SaveStateManager.LoadSlot(cmd.slot);
                        break;
                    default:
                        Debug.LogWarning($"[Argus] Unknown action: {cmd.action}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Argus] InputInjector parse error: {ex.Message}");
            }
        }

        private static void SimulateTap(float normX, float normY)
        {
            var pos = new Vector2(normX * Screen.width, normY * Screen.height);
            var data = new PointerEventData(EventSystem.current) { position = pos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            foreach (var r in results)
            {
                ExecuteEvents.Execute(r.gameObject, data, ExecuteEvents.pointerClickHandler);
                break;
            }
        }

        private static void SimulateKey(string keyName)
        {
            // NativeInput injection requires input system bridging in production;
            // for the dev emulator environment we use the legacy Input shim.
            if (System.Enum.TryParse<KeyCode>(keyName, true, out var kc))
                InputShim.SimulateKey(kc);
        }

        // ---------------------------------------------------------------
        [System.Serializable]
        private class ActionCommand
        {
            public string action;
            public float  x, y, x0, y0, x1, y1, duration, value, seconds;
            public string key, axis;
            public int    slot;
        }
    }
}
