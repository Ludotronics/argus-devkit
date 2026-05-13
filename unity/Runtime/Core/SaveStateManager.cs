// Argus SDK — SaveStateManager (Test mode only)
// Minimal save/load state API used by the agent to checkpoint and
// restore game state for deterministic repro of bugs.
//
// For games with existing save systems: hook into ArgusSession's
// OnSaveState / OnLoadState events instead of implementing from scratch.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Argus.SDK
{
    public static class SaveStateManager
    {
        private static readonly Dictionary<int, SaveSlotData> _slots = new();

        public static System.Action<int>                  OnSaveRequested;
        public static System.Action<int>                  OnLoadRequested;

        public static void SaveSlot(int slot)
        {
            // If the game has registered a handler, delegate to it
            if (OnSaveRequested != null)
            {
                OnSaveRequested.Invoke(slot);
                return;
            }
            _slots[slot] = CaptureDefault();
            Debug.Log($"[Argus] State saved to slot {slot} (rng_seed={RngCapture.CapturedSeed})");
        }

        public static void LoadSlot(int slot)
        {
            if (OnLoadRequested != null)
            {
                OnLoadRequested.Invoke(slot);
                return;
            }
            if (_slots.TryGetValue(slot, out var data))
            {
                SceneManager.LoadScene(data.sceneName);
                Debug.Log($"[Argus] State loaded from slot {slot} (scene reload). Snapshot hash was {data.stateHash}");
            }
        }

        private static SaveSlotData CaptureDefault()
        {
            var data = new SaveSlotData
            {
                sceneName = SceneManager.GetActiveScene().name,
                rngSeed = RngCapture.CapturedSeed,
            };
            if (AutomationBridgeRegistry.TryGet(out var bridge))
            {
                data.gameStateJson = bridge.GetStateSnapshotJson();
                data.stateHash = bridge.GetStateHash();
            }
            return data;
        }

        private class SaveSlotData
        {
            public string sceneName;
            public int rngSeed;
            public string gameStateJson;
            public string stateHash;
        }
    }
}
