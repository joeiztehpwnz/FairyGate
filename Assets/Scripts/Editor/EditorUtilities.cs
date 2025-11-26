using UnityEngine;
using UnityEditor;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Utility methods for editor operations.
    /// Provides helper functions for SerializedObject property manipulation.
    /// </summary>
    public static class EditorUtilities
    {
        /// <summary>
        /// Sets a serialized property value on a component using reflection.
        /// Supports various property types including primitives, vectors, colors, objects, and arrays.
        /// </summary>
        /// <param name="component">The component to modify</param>
        /// <param name="propertyName">Name of the serialized property</param>
        /// <param name="value">Value to set</param>
        public static void SetSerializedProperty(Component component, string propertyName, object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                if (value is bool boolVal)
                    prop.boolValue = boolVal;
                else if (value is int intVal)
                    prop.intValue = intVal;
                else if (value is float floatVal)
                    prop.floatValue = floatVal;
                else if (value is string stringVal)
                    prop.stringValue = stringVal;
                else if (value is Vector2 vec2Val)
                    prop.vector2Value = vec2Val;
                else if (value is Vector3 vec3Val)
                    prop.vector3Value = vec3Val;
                else if (value is Color colorVal)
                    prop.colorValue = colorVal;
                else if (value is UnityEngine.Object objVal)
                    prop.objectReferenceValue = objVal;
                else if (value is UnityEngine.Object[] arrayVal)
                {
                    prop.arraySize = arrayVal.Length;
                    for (int i = 0; i < arrayVal.Length; i++)
                    {
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = arrayVal[i];
                    }
                }

                so.ApplyModifiedProperties();
            }
        }
    }
}
