using UnityEngine;
using UnityEditor;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for PatternNode that displays nodes in a collapsible,
    /// easy-to-read format with visual organization.
    /// </summary>
    [CustomPropertyDrawer(typeof(PatternNode))]
    public class PatternNodeDrawer : PropertyDrawer
    {
        private const float FIELD_HEIGHT = 18f;
        private const float PADDING = 2f;
        private const float HEADER_HEIGHT = 24f;
        private const float SECTION_SPACING = 8f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return HEADER_HEIGHT;

            float height = HEADER_HEIGHT; // Header
            height += PADDING;

            // Basic info section
            height += FIELD_HEIGHT; // Section label
            height += FIELD_HEIGHT + PADDING; // Node name
            SerializedProperty descProp = property.FindPropertyRelative("description");
            height += EditorGUI.GetPropertyHeight(descProp, true) + PADDING; // Description (dynamic height)
            height += FIELD_HEIGHT + PADDING; // Skill type
            height += FIELD_HEIGHT + PADDING; // Requires charge
            height += SECTION_SPACING;

            // Conditions section
            SerializedProperty conditionsProp = property.FindPropertyRelative("conditions");
            height += FIELD_HEIGHT; // Section label
            if (conditionsProp != null && conditionsProp.arraySize > 0)
            {
                for (int i = 0; i < conditionsProp.arraySize; i++)
                {
                    SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(conditionProp, true) + PADDING;
                }
                height += FIELD_HEIGHT + PADDING; // Remove button space
            }
            else
            {
                height += FIELD_HEIGHT + PADDING; // "None" label
            }
            height += FIELD_HEIGHT + PADDING; // Add condition button
            height += SECTION_SPACING;

            // Transitions section
            SerializedProperty transitionsProp = property.FindPropertyRelative("transitions");
            height += FIELD_HEIGHT; // Section label
            if (transitionsProp != null && transitionsProp.arraySize > 0)
            {
                for (int i = 0; i < transitionsProp.arraySize; i++)
                {
                    SerializedProperty transitionProp = transitionsProp.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(transitionProp, true) + PADDING;
                }
            }
            else
            {
                height += FIELD_HEIGHT + PADDING; // "None" label
            }
            height += FIELD_HEIGHT + PADDING; // Add transition button
            height += SECTION_SPACING;

            // Telegraph section (optional, collapsed by default)
            height += FIELD_HEIGHT + PADDING; // Collapsed telegraph

            // Fallback section
            height += FIELD_HEIGHT; // Section label
            height += FIELD_HEIGHT + PADDING; // Fallback node
            height += FIELD_HEIGHT + PADDING; // Fallback timeout

            return height + PADDING * 4;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get properties
            SerializedProperty nameProp = property.FindPropertyRelative("nodeName");
            SerializedProperty descProp = property.FindPropertyRelative("description");
            SerializedProperty skillProp = property.FindPropertyRelative("skillToUse");
            SerializedProperty chargeProp = property.FindPropertyRelative("requiresCharge");
            SerializedProperty conditionsProp = property.FindPropertyRelative("conditions");
            SerializedProperty transitionsProp = property.FindPropertyRelative("transitions");
            SerializedProperty telegraphProp = property.FindPropertyRelative("telegraph");
            SerializedProperty fallbackNameProp = property.FindPropertyRelative("fallbackNodeName");
            SerializedProperty fallbackTimeoutProp = property.FindPropertyRelative("fallbackTimeout");

            float yPos = position.y;

            // Header with colored background
            Rect headerRect = new Rect(position.x, yPos, position.width, HEADER_HEIGHT);
            string skillName = ((SkillType)skillProp.enumValueIndex).ToString();
            string headerLabel = $"▼ {nameProp.stringValue} ({skillName})";

            // Color code by skill type
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = GetSkillColor((SkillType)skillProp.enumValueIndex);

            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, headerLabel, true, EditorStyles.foldoutHeader);
            GUI.backgroundColor = originalColor;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            yPos += HEADER_HEIGHT + PADDING;
            EditorGUI.indentLevel++;

            // === BASIC INFO SECTION ===
            DrawLabel(new Rect(position.x, yPos, position.width, FIELD_HEIGHT), "Basic Info", true);
            yPos += FIELD_HEIGHT;

            // Node name
            Rect nameRect = new Rect(position.x, yPos, position.width, FIELD_HEIGHT);
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Node Name"));
            yPos += FIELD_HEIGHT + PADDING;

            // Description
            float descHeight = EditorGUI.GetPropertyHeight(descProp, true);
            Rect descRect = new Rect(position.x, yPos, position.width, descHeight);
            EditorGUI.PropertyField(descRect, descProp, new GUIContent("Description"), true);
            yPos += descHeight + PADDING;

            // Skill type
            Rect skillRect = new Rect(position.x, yPos, position.width, FIELD_HEIGHT);
            EditorGUI.PropertyField(skillRect, skillProp, new GUIContent("Skill To Use"));
            yPos += FIELD_HEIGHT + PADDING;

            // Requires charge
            Rect chargeRect = new Rect(position.x, yPos, position.width, FIELD_HEIGHT);
            EditorGUI.PropertyField(chargeRect, chargeProp, new GUIContent("Requires Charge", "Should AI charge this skill before executing?"));
            yPos += FIELD_HEIGHT + PADDING;

            // === CONDITIONS SECTION ===
            DrawLabel(new Rect(position.x, yPos, position.width, FIELD_HEIGHT), "Execution Conditions (ALL must be true)", true);
            yPos += FIELD_HEIGHT;

            if (conditionsProp.arraySize == 0)
            {
                Rect noneRect = new Rect(position.x + 15, yPos, position.width - 15, FIELD_HEIGHT);
                EditorGUI.LabelField(noneRect, "None (always execute)");
                yPos += FIELD_HEIGHT + PADDING;
            }
            else
            {
                for (int i = 0; i < conditionsProp.arraySize; i++)
                {
                    SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(i);
                    float conditionHeight = EditorGUI.GetPropertyHeight(conditionProp, true);

                    Rect conditionRect = new Rect(position.x + 15, yPos, position.width - 40, conditionHeight);
                    EditorGUI.PropertyField(conditionRect, conditionProp, new GUIContent($"✓ {i + 1}"), true);

                    // Remove button
                    Rect removeRect = new Rect(position.x + position.width - 20, yPos, 20, FIELD_HEIGHT);
                    if (GUI.Button(removeRect, "×"))
                    {
                        conditionsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    yPos += conditionHeight + PADDING;
                }
            }

            // Add condition button
            Rect addConditionRect = new Rect(position.x + 15, yPos, 120, FIELD_HEIGHT);
            if (GUI.Button(addConditionRect, "+ Add Condition"))
            {
                conditionsProp.arraySize++;
            }
            yPos += FIELD_HEIGHT + PADDING;

            // === TRANSITIONS SECTION ===
            DrawLabel(new Rect(position.x, yPos, position.width, FIELD_HEIGHT), "Transitions (evaluated by priority)", true);
            yPos += FIELD_HEIGHT;

            if (transitionsProp.arraySize == 0)
            {
                Rect noneRect = new Rect(position.x + 15, yPos, position.width - 15, FIELD_HEIGHT);
                EditorGUI.HelpBox(noneRect, "⚠ No transitions - node is a dead end!", MessageType.Warning);
                yPos += FIELD_HEIGHT + PADDING;
            }
            else
            {
                for (int i = 0; i < transitionsProp.arraySize; i++)
                {
                    SerializedProperty transitionProp = transitionsProp.GetArrayElementAtIndex(i);
                    float transitionHeight = EditorGUI.GetPropertyHeight(transitionProp, true);

                    Rect transitionRect = new Rect(position.x + 15, yPos, position.width - 40, transitionHeight);
                    EditorGUI.PropertyField(transitionRect, transitionProp, GUIContent.none, true);

                    // Remove button
                    Rect removeRect = new Rect(position.x + position.width - 20, yPos, 20, FIELD_HEIGHT);
                    if (GUI.Button(removeRect, "×"))
                    {
                        transitionsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    yPos += transitionHeight + PADDING;
                }
            }

            // Add transition button
            Rect addTransitionRect = new Rect(position.x + 15, yPos, 120, FIELD_HEIGHT);
            if (GUI.Button(addTransitionRect, "+ Add Transition"))
            {
                transitionsProp.arraySize++;
            }
            yPos += FIELD_HEIGHT + PADDING;

            // === TELEGRAPH SECTION (Collapsed) ===
            Rect telegraphRect = new Rect(position.x, yPos, position.width, FIELD_HEIGHT);
            EditorGUI.PropertyField(telegraphRect, telegraphProp, new GUIContent("Telegraph (Optional)"), false);
            yPos += FIELD_HEIGHT + PADDING;

            // === FALLBACK SECTION ===
            DrawLabel(new Rect(position.x, yPos, position.width, FIELD_HEIGHT), "Fallback (if stuck)", false);
            yPos += FIELD_HEIGHT;

            Rect fallbackNameRect = new Rect(position.x + 15, yPos, position.width - 15, FIELD_HEIGHT);
            EditorGUI.PropertyField(fallbackNameRect, fallbackNameProp, new GUIContent("Fallback Node"));
            yPos += FIELD_HEIGHT + PADDING;

            Rect fallbackTimeRect = new Rect(position.x + 15, yPos, position.width - 15, FIELD_HEIGHT);
            EditorGUI.PropertyField(fallbackTimeRect, fallbackTimeoutProp, new GUIContent("Timeout (seconds)"));

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void DrawLabel(Rect rect, string text, bool bold)
        {
            GUIStyle style = bold ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUI.LabelField(rect, text, style);
        }

        private Color GetSkillColor(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Attack:
                    return new Color(0.7f, 0.9f, 1f); // Light blue
                case SkillType.Smash:
                    return new Color(1f, 0.7f, 0.7f); // Light red
                case SkillType.Defense:
                    return new Color(0.7f, 0.7f, 1f); // Light purple
                case SkillType.Counter:
                    return new Color(1f, 0.7f, 1f); // Light pink
                case SkillType.Windmill:
                    return new Color(0.7f, 1f, 0.7f); // Light green
                case SkillType.Lunge:
                    return new Color(1f, 1f, 0.7f); // Light yellow
                case SkillType.RangedAttack:
                    return new Color(1f, 0.9f, 0.7f); // Light orange
                default:
                    return Color.white;
            }
        }
    }
}
