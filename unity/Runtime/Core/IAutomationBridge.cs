namespace Argus.SDK
{
    /// <summary>
    /// Game automation contract for human-like Test mode.
    /// Implement on a MonoBehaviour in the game (e.g. ArgusGameBridge).
    /// </summary>
    public interface IAutomationBridge
    {
        /// <summary>Stable hash for snapshot dedup (short string).</summary>
        string GetStateHash();

        /// <summary>JSON object describing current game state (no outer array).</summary>
        string GetStateSnapshotJson();

        /// <summary>JSON array of legal semantic actions for the agent.</summary>
        string GetLegalActionsJson();

        /// <summary>
        /// Apply one semantic action. Payload is JSON with at least an "action" field.
        /// Returns JSON object: {"ok":true} or {"ok":false,"error":"...","detail":"..."}.
        /// </summary>
        string ApplySemanticAction(string actionJson);
    }
}
