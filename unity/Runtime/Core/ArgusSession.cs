// Argus SDK — ArgusSession
// Entry point. Add to a persistent GameObject in your first scene,
// assign an ArgusConfig, done.
//
//   DontDestroyOnLoad is applied automatically.
//   All subsystems are initialised/torn down here.

using System;
using System.Collections;
using UnityEngine;

namespace Argus.SDK
{
    [AddComponentMenu("Argus/ArgusSession")]
    [DisallowMultipleComponent]
    public class ArgusSession : MonoBehaviour
    {
        [SerializeField] private ArgusConfig config;

        public static ArgusSession Instance { get; private set; }
        public static ArgusMode Mode => Instance != null ? Instance.config.mode : ArgusMode.Off;
        public static string RuntimeMetadataJson => Instance != null ? Instance.BuildRuntimeMetadataJson() : "{}";

        // Subsystems (lazy initialised per mode)
        private StateStreamer _stateStreamer;
        private InputInjector  _inputInjector;
        private RngCapture     _rngCapture;
        private LiveTelemetry  _liveTelemetry;
        private KillSwitch     _killSwitch;

        // ---------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
            {
                Debug.LogError("[Argus] No ArgusConfig assigned. SDK is disabled.");
                return;
            }

#if ARGUS_STRIP_TEST
            // Post-build processor strips Test-mode code from Release builds
            if (config.mode == ArgusMode.Test)
            {
                Debug.LogWarning("[Argus] Test mode stripped from this build.");
                config.mode = ArgusMode.Off;
            }
#endif

            switch (config.mode)
            {
                case ArgusMode.Off:
                    break;
                case ArgusMode.Test:
                    InitTestMode();
                    break;
                case ArgusMode.Live:
                    InitLiveMode();
                    break;
            }
        }

        private void InitTestMode()
        {
            Debug.Log("[Argus] Test mode starting.");
            _killSwitch    = gameObject.AddComponent<KillSwitch>();
            _stateStreamer  = gameObject.AddComponent<StateStreamer>();
            _inputInjector  = gameObject.AddComponent<InputInjector>();
            _rngCapture     = gameObject.AddComponent<RngCapture>();

            _killSwitch.Init(config);
            _stateStreamer.Init(config);
            _inputInjector.Init(config);
            _rngCapture.Init();
        }

        private void InitLiveMode()
        {
            Debug.Log("[Argus] Live mode starting.");
            _killSwitch   = gameObject.AddComponent<KillSwitch>();
            _liveTelemetry = gameObject.AddComponent<LiveTelemetry>();

            _killSwitch.Init(config);
            _liveTelemetry.Init(config);
            _liveTelemetry.RecordEvent("sdk_health", new
            {
                status = "initialized",
                mode = "live",
                schema_version = "1.0.0",
                runtime = RuntimeMetadataJson
            });
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ---------------------------------------------------------------
        // Public API — safe to call in any mode; no-ops in Off mode
        // ---------------------------------------------------------------

        /// <summary>Record a custom named event (Live + Test modes).</summary>
        public static void Event(string name, object properties = null)
        {
            if (Mode == ArgusMode.Off) return;
            if (Mode == ArgusMode.Live)
                Instance?._liveTelemetry?.RecordEvent(name, properties);
            // In Test mode the agent observes events via the state stream
        }

        /// <summary>Record a purchase for monetisation testing.</summary>
        public static void Purchase(string sku, decimal price, string currency)
        {
            Event("iap_purchase", new { sku, price, currency });
        }

        /// <summary>Set user consent for Live telemetry (GDPR/CCPA).</summary>
        public static void SetConsent(bool granted)
        {
            if (Mode == ArgusMode.Live)
                Instance?._liveTelemetry?.SetConsent(granted);
        }

        private string BuildRuntimeMetadataJson()
        {
            var orientation = Screen.width >= Screen.height ? "landscape" : "portrait";
            return "{" +
                   "\"engine\":\"unity\"," +
                   $"\"engine_version\":\"{Application.unityVersion}\"," +
                   $"\"platform\":\"{Application.platform.ToString().ToLowerInvariant()}\"," +
                   $"\"package_id\":\"{Application.identifier}\"," +
                   $"\"orientation\":\"{orientation}\"," +
                   "\"sdk_schema_version\":\"1.0.0\"," +
                   $"\"sdk_mode\":\"{config.mode.ToString().ToLowerInvariant()}\"" +
                   "}";
        }
    }
}
