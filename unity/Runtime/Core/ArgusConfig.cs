// Argus SDK — ArgusConfig
// ScriptableObject holding all SDK configuration.
// Create via: Assets > Create > Argus > Config

using UnityEngine;

namespace Argus.SDK
{
    public enum ArgusMode
    {
        Off,   // No-op. All SDK calls are stripped at compile time in Release builds.
        Test,  // Full instrumentation: state streaming, input injection, RNG capture.
        Live,  // Lightweight production telemetry: crashes, perf, events. No state stream.
    }

    [CreateAssetMenu(fileName = "ArgusConfig", menuName = "Argus/Config", order = 1)]
    public class ArgusConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Your Argus project API key.")]
        public string apiKey = "";

        [Tooltip("Argus backend URL.")]
        public string backendUrl = "https://api.argus.dev";

        [Header("Mode")]
        [Tooltip("Off = disabled, Test = dev/CI instrumentation, Live = production telemetry.")]
        public ArgusMode mode = ArgusMode.Off;

        [Header("Test Mode")]
        [Tooltip("WebSocket port the backend agent connects to.")]
        public int testWsPort = 7777;

        [Tooltip("State snapshot interval in seconds (0.1 – 1.0 recommended).")]
        [Range(0.1f, 2.0f)]
        public float snapshotIntervalSecs = 0.25f;

        [Header("Live Mode — Resource Budget")]
        [Tooltip("Max RAM the SDK may use (MB). SDK self-terminates if exceeded.")]
        [Range(1, 10)]
        public int maxRamMb = 5;

        [Tooltip("Max CPU % (main thread) the SDK may consume.")]
        [Range(0.1f, 2.0f)]
        public float maxCpuPct = 1.0f;

        [Tooltip("Max outbound network per session (KB).")]
        [Range(10, 500)]
        public int maxNetKb = 100;

        [Header("Live Mode — Privacy")]
        [Tooltip("Require explicit user opt-in before sending any telemetry.")]
        public bool requireConsentOptIn = true;

        [Tooltip("Never capture screenshots in Live mode.")]
        public bool noScreenshotsInLive = true;

        [Tooltip("Redact PII patterns (email, phone) from captured events.")]
        public bool piiRedaction = true;
    }
}
