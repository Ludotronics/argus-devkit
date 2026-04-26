// Argus SDK — InputShim (Test mode only)
// Thin bridge that translates Argus agent commands to Unity's input systems.
// Supports both Legacy Input Manager and the new Input System package.

using UnityEngine;

namespace Argus.SDK
{
    internal static class InputShim
    {
        public static void SimulateKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            // New Input System: use InputEventPtr simulation
            // (requires com.unity.inputsystem >= 1.7)
            try
            {
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null)
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(
                        keyboard,
                        new UnityEngine.InputSystem.LowLevel.KeyboardState(
                            (UnityEngine.InputSystem.Key)(int)key));
            }
            catch { /* Input System not available in this build */ }
#else
            // Legacy Input Manager: no direct key injection API.
            // The agent should use UI tap simulation via InputInjector for
            // button interactions rather than raw key codes.
            Debug.LogWarning($"[Argus] SimulateKey({key}) not supported in Legacy Input Manager. Use tap action instead.");
#endif
        }
    }
}
