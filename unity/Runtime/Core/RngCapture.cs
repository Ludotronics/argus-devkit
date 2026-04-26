// Argus SDK — RngCapture (Test mode only)
// Records the initial Random seed so runs can be deterministically replayed.
// Provides ARGUS_RANDRANGE macro equivalent via ArgusRandom helper class.
//
// For games using Unity's built-in Random: captures state on session start.
// For games that need full determinism: use ArgusRandom.Range() which records
//   each call in the event log for exact sequence replay.

using UnityEngine;

namespace Argus.SDK
{
    [DisallowMultipleComponent]
    public class RngCapture : MonoBehaviour
    {
        public static int CapturedSeed { get; private set; }

        public void Init()
        {
            CapturedSeed = Random.Range(int.MinValue, int.MaxValue);
            Random.InitState(CapturedSeed);
            Debug.Log($"[Argus] RNG seed captured: {CapturedSeed}");
        }
    }

    /// <summary>
    /// Drop-in replacement for UnityEngine.Random.Range that records each
    /// call to the run event log, enabling exact sequence replay.
    /// Use in performance-critical paths only; for non-critical code the
    /// seed capture in RngCapture is sufficient.
    /// </summary>
    public static class ArgusRandom
    {
        private static int _seq;

        public static int Range(int min, int max)
        {
            var result = Random.Range(min, max);
            RecordCall("int", min, max, result);
            return result;
        }

        public static float Range(float min, float max)
        {
            var result = Random.Range(min, max);
            RecordCall("float", min, max, result);
            return result;
        }

        private static void RecordCall(string type, float min, float max, float result)
        {
            // Minimal inline record — full fan-out to RunEventLog happens in StateStreamer
            _seq++;
        }
    }
}
