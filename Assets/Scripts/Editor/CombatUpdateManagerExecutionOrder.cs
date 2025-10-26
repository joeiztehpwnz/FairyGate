using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Editor
{
    /// <summary>
    /// Ensures CombatUpdateManager executes before other combat systems.
    /// This runs automatically when Unity compiles scripts.
    /// </summary>
    [InitializeOnLoad]
    public class CombatUpdateManagerExecutionOrder
    {
        static CombatUpdateManagerExecutionOrder()
        {
            // Set CombatUpdateManager to execute early (before default time of 0)
            SetExecutionOrder<CombatUpdateManager>(-100);

            Debug.Log("[CombatUpdateManagerExecutionOrder] Static constructor executed during compilation");
        }

        private static void SetExecutionOrder<T>(int order) where T : MonoBehaviour
        {
            string scriptName = typeof(T).Name;

            // Find the MonoScript for this type
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == typeof(T))
                {
                    // Only set if it's not already set correctly
                    if (MonoImporter.GetExecutionOrder(monoScript) != order)
                    {
                        MonoImporter.SetExecutionOrder(monoScript, order);
                        Debug.Log($"[Script Execution Order] Set {scriptName} to {order}");
                    }
                    return;
                }
            }
        }
    }
}
