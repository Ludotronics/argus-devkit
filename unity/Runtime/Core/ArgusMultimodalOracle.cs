// Argus SDK — lightweight multimodal signals (Test mode).
// Captures frame timing spikes and optional screenshot hooks for runners/backends.

using UnityEngine;

namespace Argus.SDK
{
    [DisallowMultipleComponent]
    public class ArgusMultimodalOracle : MonoBehaviour
    {
        private float _emaDt;
        private int _spikeCount;

        public float LastDeltaTimeMs { get; private set; }
        public float SpikeEmaMs { get; private set; }
        public int SpikeCount => _spikeCount;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * 1000f;
            LastDeltaTimeMs = dt;
            _emaDt = _emaDt <= 0f ? dt : Mathf.Lerp(_emaDt, dt, 0.05f);
            if (dt > Mathf.Max(50f, _emaDt * 3f))
                _spikeCount++;
            SpikeEmaMs = _emaDt;
        }

        /// <summary>Optional hook: capture screenshot bytes via Unity API in a later phase.</summary>
        public static byte[] CaptureScreenshotPngIfAvailable()
        {
            if (ArgusSession.Mode != ArgusMode.Test)
                return null;
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) return null;
            try
            {
                return ImageConversion.EncodeToPNG(tex);
            }
            finally
            {
                Destroy(tex);
            }
        }
    }
}
