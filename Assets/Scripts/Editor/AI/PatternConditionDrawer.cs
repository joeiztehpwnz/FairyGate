using UnityEngine;
using UnityEditor;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for PatternCondition that only shows relevant fields
    /// based on the selected condition type, making the Inspector much cleaner.
    /// </summary>
    [CustomPropertyDrawer(typeof(PatternCondition))]
    public class PatternConditionDrawer : PropertyDrawer
    {
        private const float FIELD_HEIGHT = 18f;
        private const float PADDING = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Type dropdown + value field
            return FIELD_HEIGHT * 2 + PADDING;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects
            Rect typeRect = new Rect(position.x, position.y, position.width, FIELD_HEIGHT);
            Rect valueRect = new Rect(position.x, position.y + FIELD_HEIGHT + PADDING, position.width, FIELD_HEIGHT);

            // Get properties
            SerializedProperty typeProp = property.FindPropertyRelative("type");
            SerializedProperty floatProp = property.FindPropertyRelative("floatValue");
            SerializedProperty intProp = property.FindPropertyRelative("intValue");
            SerializedProperty boolProp = property.FindPropertyRelative("boolValue");
            SerializedProperty skillProp = property.FindPropertyRelative("skillValue");
            SerializedProperty combatStateProp = property.FindPropertyRelative("combatStateValue");

            // Draw condition type dropdown
            EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Condition Type"));

            // Draw appropriate value field based on type
            ConditionType type = (ConditionType)typeProp.enumValueIndex;
            DrawValueField(valueRect, type, floatProp, intProp, boolProp, skillProp, combatStateProp);

            EditorGUI.EndProperty();
        }

        private void DrawValueField(Rect rect, ConditionType type,
            SerializedProperty floatProp, SerializedProperty intProp, SerializedProperty boolProp,
            SerializedProperty skillProp, SerializedProperty combatStateProp)
        {
            switch (type)
            {
                // Health-based (percentage)
                case ConditionType.HealthAbove:
                    floatProp.floatValue = EditorGUI.Slider(rect, "HP Threshold %", floatProp.floatValue, 0f, 100f);
                    break;

                case ConditionType.HealthBelow:
                    floatProp.floatValue = EditorGUI.Slider(rect, "HP Threshold %", floatProp.floatValue, 0f, 100f);
                    break;

                // Hit tracking (count)
                case ConditionType.HitsTakenCount:
                    intProp.intValue = EditorGUI.IntField(rect, "Minimum Hits Taken", intProp.intValue);
                    break;

                case ConditionType.HitsDealtCount:
                    intProp.intValue = EditorGUI.IntField(rect, "Minimum Hits Dealt", intProp.intValue);
                    break;

                // Player state
                case ConditionType.PlayerCharging:
                    boolProp.boolValue = EditorGUI.Toggle(rect, "Player Is Charging", boolProp.boolValue);
                    break;

                case ConditionType.PlayerSkillType:
                    EditorGUI.PropertyField(rect, skillProp, new GUIContent("Player Skill"));
                    break;

                case ConditionType.PlayerCombatState:
                    EditorGUI.PropertyField(rect, combatStateProp, new GUIContent("Player Combat State"));
                    break;

                case ConditionType.PlayerInRange:
                    floatProp.floatValue = EditorGUI.FloatField(rect, "Max Distance", floatProp.floatValue);
                    break;

                case ConditionType.WeaponInRange:
                    EditorGUI.LabelField(rect, "Check Weapon Range", "(Automatic - no value needed)");
                    break;

                // Self state
                case ConditionType.StaminaAbove:
                    floatProp.floatValue = EditorGUI.FloatField(rect, "Minimum Stamina", floatProp.floatValue);
                    break;

                case ConditionType.StaminaBelow:
                    floatProp.floatValue = EditorGUI.FloatField(rect, "Maximum Stamina", floatProp.floatValue);
                    break;

                case ConditionType.SkillReady:
                    EditorGUI.PropertyField(rect, skillProp, new GUIContent("Skill"));
                    break;

                // Timing
                case ConditionType.TimeElapsed:
                    floatProp.floatValue = EditorGUI.FloatField(rect, "Seconds Elapsed", floatProp.floatValue);
                    break;

                case ConditionType.CooldownExpired:
                    intProp.intValue = EditorGUI.IntField(rect, "Cooldown ID", intProp.intValue);
                    break;

                // Random
                case ConditionType.RandomChance:
                    float chance = floatProp.floatValue;
                    chance = EditorGUI.Slider(rect, "Chance %", chance * 100f, 0f, 100f) / 100f;
                    floatProp.floatValue = Mathf.Clamp01(chance);
                    break;

                default:
                    EditorGUI.LabelField(rect, "Unknown condition type");
                    break;
            }
        }
    }
}
