using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Custom Editor for PatternDefinition that provides an enhanced, designer-friendly
    /// UI for creating and editing AI behavior patterns.
    /// </summary>
    [CustomEditor(typeof(PatternDefinition))]
    public class PatternDefinitionEditor : UnityEditor.Editor
    {
        private ReorderableList nodesList;
        private SerializedProperty patternNameProp;
        private SerializedProperty archetypeTagProp;
        private SerializedProperty designNotesProp;
        private SerializedProperty nodesProp;
        private SerializedProperty startingNodeNameProp;
        private SerializedProperty difficultyTierProp;
        private SerializedProperty enableDebugLogsProp;

        private PatternDefinition pattern;
        private List<string> validationErrors;

        private void OnEnable()
        {
            pattern = (PatternDefinition)target;

            // Get serialized properties
            patternNameProp = serializedObject.FindProperty("patternName");
            archetypeTagProp = serializedObject.FindProperty("archetypeTag");
            designNotesProp = serializedObject.FindProperty("designNotes");
            nodesProp = serializedObject.FindProperty("nodes");
            startingNodeNameProp = serializedObject.FindProperty("startingNodeName");
            difficultyTierProp = serializedObject.FindProperty("difficultyTier");
            enableDebugLogsProp = serializedObject.FindProperty("enableDebugLogs");

            // Create reorderable list for nodes
            nodesList = new ReorderableList(serializedObject, nodesProp, true, true, true, true);
            nodesList.drawHeaderCallback = DrawNodesHeader;
            nodesList.drawElementCallback = DrawNodeElement;
            nodesList.elementHeightCallback = GetNodeElementHeight;
            nodesList.onAddCallback = OnAddNode;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // === HEADER SECTION ===
            DrawHeader();
            EditorGUILayout.Space(10);

            // === PATTERN INFO SECTION ===
            DrawPatternInfo();
            EditorGUILayout.Space(10);

            // === NODES SECTION ===
            DrawNodesSection();
            EditorGUILayout.Space(10);

            // === VALIDATION SECTION ===
            DrawValidationSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Title with pattern name
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField($"AI Pattern: {patternNameProp.stringValue}", titleStyle);

            // Archetype and difficulty
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Archetype: {archetypeTagProp.stringValue}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Difficulty: {GetDifficultyStars(difficultyTierProp.intValue)}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawPatternInfo()
        {
            EditorGUILayout.LabelField("Pattern Info", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(patternNameProp, new GUIContent("Pattern Name"));
            EditorGUILayout.PropertyField(archetypeTagProp, new GUIContent("Archetype"));
            EditorGUILayout.PropertyField(difficultyTierProp, new GUIContent("Difficulty Tier"));
            EditorGUILayout.PropertyField(designNotesProp, new GUIContent("Design Notes"), GUILayout.Height(60));
            EditorGUILayout.PropertyField(enableDebugLogsProp, new GUIContent("Enable Debug Logs"));
        }

        private void DrawNodesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Pattern Nodes ({nodesProp.arraySize})", EditorStyles.boldLabel);

            // Starting node dropdown
            DrawStartingNodeDropdown();

            EditorGUILayout.Space(5);

            // Reorderable nodes list
            nodesList.DoLayoutList();

            // Quick add buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Attack Node", GUILayout.Height(25)))
            {
                AddNodeWithDefaults("New Attack", SkillType.Attack, false);
            }
            if (GUILayout.Button("+ Add Smash Node", GUILayout.Height(25)))
            {
                AddNodeWithDefaults("New Smash", SkillType.Smash, true);
            }
            if (GUILayout.Button("+ Add Defense Node", GUILayout.Height(25)))
            {
                AddNodeWithDefaults("New Defense", SkillType.Defense, false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStartingNodeDropdown()
        {
            // Get list of node names for dropdown
            List<string> nodeNames = new List<string>();
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = nodeProp.FindPropertyRelative("nodeName");
                nodeNames.Add(nameProperty.stringValue);
            }

            if (nodeNames.Count == 0)
            {
                EditorGUILayout.HelpBox("⚠ No nodes defined. Add a node to set starting node.", MessageType.Warning);
                return;
            }

            // Find current index
            int currentIndex = nodeNames.IndexOf(startingNodeNameProp.stringValue);
            if (currentIndex == -1) currentIndex = 0;

            // Dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Starting Node:", GUILayout.Width(100));
            int newIndex = EditorGUILayout.Popup(currentIndex, nodeNames.ToArray());
            if (newIndex != currentIndex)
            {
                startingNodeNameProp.stringValue = nodeNames[newIndex];
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pattern Validation", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate Pattern", GUILayout.Width(120)))
            {
                ValidatePattern();
            }
            EditorGUILayout.EndHorizontal();

            if (validationErrors != null && validationErrors.Count > 0)
            {
                EditorGUILayout.Space(5);
                foreach (string error in validationErrors)
                {
                    EditorGUILayout.HelpBox($"⚠ {error}", MessageType.Error);
                }
            }
            else if (validationErrors != null)
            {
                EditorGUILayout.HelpBox("✓ Pattern is valid!", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        // === REORDERABLE LIST CALLBACKS ===

        private void DrawNodesHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Nodes (drag to reorder)");
        }

        private void DrawNodeElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, nodeProp, GUIContent.none, true);
        }

        private float GetNodeElementHeight(int index)
        {
            SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(nodeProp, true) + 4;
        }

        private void OnAddNode(ReorderableList list)
        {
            // Add new node with defaults
            nodesProp.arraySize++;
            SerializedProperty newNodeProp = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);

            // Set default values
            newNodeProp.FindPropertyRelative("nodeName").stringValue = $"Node {nodesProp.arraySize}";
            newNodeProp.FindPropertyRelative("description").stringValue = "";
            newNodeProp.FindPropertyRelative("skillToUse").enumValueIndex = 0; // Attack
            newNodeProp.FindPropertyRelative("requiresCharge").boolValue = false;
            newNodeProp.FindPropertyRelative("fallbackNodeName").stringValue = "";
            newNodeProp.FindPropertyRelative("fallbackTimeout").floatValue = 5f;

            // Clear lists
            newNodeProp.FindPropertyRelative("conditions").ClearArray();
            newNodeProp.FindPropertyRelative("transitions").ClearArray();
        }

        // === HELPER METHODS ===

        private void AddNodeWithDefaults(string nodeName, SkillType skill, bool requiresCharge)
        {
            nodesProp.arraySize++;
            SerializedProperty newNodeProp = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);

            newNodeProp.FindPropertyRelative("nodeName").stringValue = nodeName;
            newNodeProp.FindPropertyRelative("skillToUse").enumValueIndex = (int)skill;
            newNodeProp.FindPropertyRelative("requiresCharge").boolValue = requiresCharge;
            newNodeProp.FindPropertyRelative("fallbackTimeout").floatValue = 5f;

            newNodeProp.FindPropertyRelative("conditions").ClearArray();
            newNodeProp.FindPropertyRelative("transitions").ClearArray();

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidatePattern()
        {
            validationErrors = new List<string>();

            // Check if pattern has nodes
            if (nodesProp.arraySize == 0)
            {
                validationErrors.Add("Pattern has no nodes defined!");
                return;
            }

            // Check starting node exists
            bool startingNodeFound = false;
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(i);
                string nodeName = nodeProp.FindPropertyRelative("nodeName").stringValue;
                if (nodeName == startingNodeNameProp.stringValue)
                {
                    startingNodeFound = true;
                    break;
                }
            }

            if (!startingNodeFound)
            {
                validationErrors.Add($"Starting node '{startingNodeNameProp.stringValue}' not found in node list!");
            }

            // Check all transition targets exist
            HashSet<string> nodeNames = new HashSet<string>();
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(i);
                string nodeName = nodeProp.FindPropertyRelative("nodeName").stringValue;
                nodeNames.Add(nodeName);
            }

            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(i);
                string nodeName = nodeProp.FindPropertyRelative("nodeName").stringValue;
                SerializedProperty transitionsProp = nodeProp.FindPropertyRelative("transitions");

                if (transitionsProp.arraySize == 0)
                {
                    validationErrors.Add($"Node '{nodeName}' has no transitions (dead end)!");
                }

                for (int j = 0; j < transitionsProp.arraySize; j++)
                {
                    SerializedProperty transitionProp = transitionsProp.GetArrayElementAtIndex(j);
                    string targetName = transitionProp.FindPropertyRelative("targetNodeName").stringValue;

                    if (string.IsNullOrEmpty(targetName))
                    {
                        validationErrors.Add($"Node '{nodeName}' has transition with empty target!");
                    }
                    else if (!nodeNames.Contains(targetName))
                    {
                        validationErrors.Add($"Node '{nodeName}' references non-existent node '{targetName}'!");
                    }
                }
            }

            Debug.Log($"[PatternDefinitionEditor] Validation complete: {validationErrors.Count} errors found");
        }

        private string GetDifficultyStars(int tier)
        {
            switch (tier)
            {
                case 1: return "⭐ (Beginner)";
                case 2: return "⭐⭐ (Intermediate)";
                case 3: return "⭐⭐⭐ (Advanced)";
                default: return "❓";
            }
        }
    }
}
