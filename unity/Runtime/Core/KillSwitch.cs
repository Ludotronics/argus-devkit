// Argus SDK — KillSwitch
// Polls a CDN-cacheable config endpoint on startup and periodically.
// If the server disables the SDK (globally, per-version, or per-cohort),
// the SDK tears itself down without affecting the game.
//
// Endpoint: GET {backendUrl}/sdk/killswitch?app={bundleId}&ver={version}
// Response: { "enabled": true, "mode": "live" }
// Cached by CDN for 5 minutes — zero latency cost at runtime.

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Argus.SDK
{
    [DisallowMultipleComponent]
    public class KillSwitch : MonoBehaviour
    {
        private ArgusConfig _config;
        private const float PollIntervalSecs = 300f;   // 5 minutes
        private const float TrustWindowSecs  = 300f;   // tolerate backend outage for 5m

        private float _lastSuccessTime;

        public void Init(ArgusConfig config)
        {
            _config = config;
            _lastSuccessTime = Time.realtimeSinceStartup;
            StartCoroutine(PollLoop());
        }

        private IEnumerator PollLoop()
        {
            // Initial check
            yield return StartCoroutine(Check());
            while (true)
            {
                yield return new WaitForSeconds(PollIntervalSecs);
                yield return StartCoroutine(Check());
            }
        }

        private IEnumerator Check()
        {
            var url = $"{_config.backendUrl}/sdk/killswitch" +
                      $"?app={Application.identifier}" +
                      $"&ver={Application.version}" +
                      $"&mode={_config.mode.ToString().ToLower()}";

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("X-Argus-Key", _config.apiKey);
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                _lastSuccessTime = Time.realtimeSinceStartup;
                var resp = JsonUtility.FromJson<KillSwitchResponse>(req.downloadHandler.text);
                if (!resp.enabled)
                {
                    Debug.LogWarning("[Argus] SDK disabled by kill switch. Shutting down.");
                    Shutdown();
                }
            }
            else
            {
                // Backend unreachable — fail-closed after trust window expires
                if (Time.realtimeSinceStartup - _lastSuccessTime > TrustWindowSecs)
                {
                    Debug.LogWarning("[Argus] Kill switch unreachable beyond trust window. Disabling SDK.");
                    Shutdown();
                }
            }
        }

        private void Shutdown()
        {
            // Destroy all SDK components but leave the game untouched
            foreach (var comp in GetComponents<MonoBehaviour>())
            {
                if (comp != this && comp.GetType().Namespace == "Argus.SDK")
                    Destroy(comp);
            }
            Destroy(this);
        }

        [System.Serializable]
        private class KillSwitchResponse
        {
            public bool   enabled = true;
            public string mode    = "live";
        }
    }
}
