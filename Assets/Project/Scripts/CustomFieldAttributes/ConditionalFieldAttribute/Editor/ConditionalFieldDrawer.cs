using UnityEngine;
using UnityEditor;

namespace GeniesIRL
{
    /// <summary>
    /// Allows us to easily mark fields as conditional in the Inspector. For example, if you want an Inspector
    /// property to only be visible if a box is ticked, you can mark the property with [ConditionalField("myBoolField")]
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
    public class ConditionalFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalFieldAttribute conditional = (ConditionalFieldAttribute)attribute;
            SerializedProperty sourceProperty = property.serializedObject.FindProperty(conditional.ConditionalSourceField);

            if (sourceProperty != null && sourceProperty.boolValue)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ConditionalFieldAttribute conditional = (ConditionalFieldAttribute)attribute;
            SerializedProperty sourceProperty = property.serializedObject.FindProperty(conditional.ConditionalSourceField);

            if (sourceProperty != null && !sourceProperty.boolValue)
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

