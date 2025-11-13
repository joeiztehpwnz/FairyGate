using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for PatternTransition that shows transitions
    /// in a clean, collapsible format with priority indicators.
    /// </summary>
    [CustomPropertyDrawer(typeof(PatternTransition))]
    public class PatternTransitionDrawer : PropertyDrawer
    {
        private const float FIELD_HEIGHT = 18f;
        private const float PADDING = 2f;
        private const float HEADER_HEIGHT = 22f;

        private static ReorderableList conditionsList;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return HEADER_HEIGHT;

            SerializedProperty conditionsProp = property.FindPropertyRelative("conditions");

            // Header + target + priority + conditions list + spacing
            float height = HEADER_HEIGHT; // Foldout header
            height += FIELD_HEIGHT + PADDING; // Target node
            height += FIELD_HEIGHT + PADDING; // Priority

            if (conditionsProp != null && conditionsProp.arraySize > 0)
            {
                height += GetConditionsListHeight(conditionsProp);
            }
            else
            {
                height += FIELD_HEIGHT + PADDING; // "No conditions" label
            }

            return height + PADDING * 2;
        }

        private float GetConditionsListHeight(SerializedProperty conditionsProp)
        {
            // Header + each condition (2 lines each)
            float height = FIELD_HEIGHT + PADDING; // List header
            height += conditionsProp.arraySize * (FIELD_HEIGHT * 2 + PADDING * 2);
            height += FIELD_HEIGHT + PADDING; // Add button footer
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get properties
            SerializedProperty targetProp = property.FindPropertyRelative("targetNodeName");
            SerializedProperty priorityProp = property.FindPropertyRelative("priority");
            SerializedProperty conditionsProp = property.FindPropertyRelative("conditions");

            float yPos = position.y;

            // Foldout header with priority indicator
            Rect headerRect = new Rect(position.x, yPos, position.width, HEADER_HEIGHT);
            string headerLabel = $"→ {targetProp.stringValue} (Priority: {priorityProp.intValue})";

            // Color code by priority
            Color originalColor = GUI.backgroundColor;
            if (priorityProp.intValue >= 10)
                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f); // Green for high priority
            else if (priorityProp.intValue >= 5)
                GUI.backgroundColor = new Color(1f, 1f, 0.5f); // Yellow for medium
            else
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f); // Gray for low

            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, headerLabel, true);
            GUI.backgroundColor = originalColor;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            yPos += HEADER_HEIGHT + PADDING;

            // Indent for foldout content
            EditorGUI.indentLevel++;

            // Target node field
            Rect targetRect = new Rect(position.x, yPos, position.width, FIELD_HEIGHT);
            EditorGUI.PropertyField(targetRect, targetProp, new GUIContent("Target Node"));
            yPos += FIELD_HEIGHT + PADDING;

            // Priority field
            Rect priorityRect = new Rect(position.x, yPos, position.width, FIELD_HEIGHT);
            EditorGUI.PropertyField(priorityRect, priorityProp, new GUIContent("Priority", "Higher priority transitions are evaluated first"));
            yPos += FIELD_HEIGHT + PADDING;

            // Conditions list
            if (conditionsProp.arraySize == 0)
            {
                Rect noConditionsRect = new Rect(position.x, yPos, position.width, FIELD_HEIGHT);
                EditorGUI.LabelField(noConditionsRect, "Conditions", "Always (no conditions)");
                yPos += FIELD_HEIGHT + PADDING;

                // Add condition button
                Rect addButtonRect = new Rect(position.x + position.width - 100, yPos, 100, FIELD_HEIGHT);
                if (GUI.Button(addButtonRect, "+ Add Condition"))
                {
                    conditionsProp.arraySize++;
                }
                yPos += FIELD_HEIGHT + PADDING;
            }
            else
            {
                // Draw conditions header
                Rect conditionsLabelRect = new Rect(position.x, yPos, position.width / 2, FIELD_HEIGHT);
                EditorGUI.LabelField(conditionsLabelRect, "Conditions (ALL must be true):");

                // Add condition button in same row
                Rect addButtonRect = new Rect(position.x + position.width - 100, yPos, 100, FIELD_HEIGHT);
                if (GUI.Button(addButtonRect, "+ Add"))
                {
                    conditionsProp.arraySize++;
                }
                yPos += FIELD_HEIGHT + PADDING;

                // Draw each condition
                for (int i = 0; i < conditionsProp.arraySize; i++)
                {
                    SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(i);

                    Rect conditionRect = new Rect(position.x, yPos, position.width - 25, FIELD_HEIGHT * 2);
                    EditorGUI.PropertyField(conditionRect, conditionProp, new GUIContent($"  ✓ Condition {i + 1}"), true);

                    // Remove button
                    Rect removeButtonRect = new Rect(position.x + position.width - 20, yPos, 20, FIELD_HEIGHT);
                    if (GUI.Button(removeButtonRect, "×"))
                    {
                        conditionsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    yPos += FIELD_HEIGHT * 2 + PADDING * 2;
                }
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
