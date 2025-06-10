
using UnityEditor;
using UnityEngine;

namespace GeniesIRL 
{
    [CustomPropertyDrawer(typeof(InspectorNoteAttribute))]
    public class InspectorNoteDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InspectorNoteAttribute note = attribute as InspectorNoteAttribute;
            EditorGUI.HelpBox(position, note.Text, MessageType.Info);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InspectorNoteAttribute note = attribute as InspectorNoteAttribute;
            return EditorGUIUtility.singleLineHeight * 3; // Adjust this multiplier as needed.
        }
    }
}