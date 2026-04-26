// Argus SDK — ArgusStateAttribute
// Mark any serializable field or property on a MonoBehaviour with [ArgusState]
// to include it in the state snapshot streamed to the Argus backend.
//
// Usage:
//   [ArgusState] public int playerHealth;
//   [ArgusState("pos")] public Vector3 transform.position;  // custom key
//
// The SDK's StateCollector reflects over all MonoBehaviours in the active
// scene and gathers annotated fields at every snapshot interval.

using System;

namespace Argus.SDK
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ArgusStateAttribute : Attribute
    {
        /// <summary>
        /// Optional override key in the JSON snapshot.
        /// Defaults to the field/property name.
        /// </summary>
        public string Key { get; }

        public ArgusStateAttribute(string key = null)
        {
            Key = key;
        }
    }
}
