namespace Argus.SDK
{
    /// <summary>
    /// Holds the game-provided automation bridge for Test mode (state + semantic actions).
    /// </summary>
    public static class AutomationBridgeRegistry
    {
        private static IAutomationBridge _instance;

        public static void Register(IAutomationBridge bridge) => _instance = bridge;

        public static void Unregister(IAutomationBridge bridge)
        {
            if (_instance == bridge)
                _instance = null;
        }

        public static bool TryGet(out IAutomationBridge bridge)
        {
            bridge = _instance;
            return bridge != null;
        }
    }
}
