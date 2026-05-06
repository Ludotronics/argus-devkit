namespace Argus.SDK
{
    /// <summary>
    /// Placeholder automation contract used in Test mode.
    /// </summary>
    public interface IAutomationBridge
    {
        string GetStateHash();
        bool InjectInput(string action, string payloadJson);
    }
}
