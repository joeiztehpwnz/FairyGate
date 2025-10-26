using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Editor
{
    public class QuickAISwapper : EditorWindow
    {
        [MenuItem("FairyGate/Quick AI Swapper")]
        public static void ShowWindow()
        {
            GetWindow<QuickAISwapper>("AI Swapper");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Quick AI Type Swapper", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (Selection.gameObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("Select one or more GameObjects with AI components in the Hierarchy", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Selected: {Selection.gameObjects.Length} object(s)");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Swap to:", EditorStyles.boldLabel);

            if (GUILayout.Button("Simple Test AI", GUILayout.Height(30)))
            {
                SwapToAI<SimpleTestAI>();
            }

            if (GUILayout.Button("Knight AI", GUILayout.Height(30)))
            {
                SwapToAI<KnightAI>();
            }

            if (GUILayout.Button("Test Repeater AI", GUILayout.Height(30)))
            {
                SwapToAI<TestRepeaterAI>();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Remove All AI", GUILayout.Height(25)))
            {
                RemoveAllAI();
            }
        }

        private void SwapToAI<T>() where T : MonoBehaviour
        {
            foreach (var obj in Selection.gameObjects)
            {
                // Remove existing AI components
                var simpleAI = obj.GetComponent<SimpleTestAI>();
                if (simpleAI != null) DestroyImmediate(simpleAI);

                var knightAI = obj.GetComponent<KnightAI>();
                if (knightAI != null) DestroyImmediate(knightAI);

                var repeaterAI = obj.GetComponent<TestRepeaterAI>();
                if (repeaterAI != null) DestroyImmediate(repeaterAI);

                // Add new AI component
                obj.AddComponent<T>();

                EditorUtility.SetDirty(obj);
            }

            Debug.Log($"[AI Swapper] Changed {Selection.gameObjects.Length} object(s) to {typeof(T).Name}");
        }

        private void RemoveAllAI()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var simpleAI = obj.GetComponent<SimpleTestAI>();
                if (simpleAI != null) DestroyImmediate(simpleAI);

                var knightAI = obj.GetComponent<KnightAI>();
                if (knightAI != null) DestroyImmediate(knightAI);

                var repeaterAI = obj.GetComponent<TestRepeaterAI>();
                if (repeaterAI != null) DestroyImmediate(repeaterAI);

                EditorUtility.SetDirty(obj);
            }

            Debug.Log($"[AI Swapper] Removed AI from {Selection.gameObjects.Length} object(s)");
        }
    }
}
