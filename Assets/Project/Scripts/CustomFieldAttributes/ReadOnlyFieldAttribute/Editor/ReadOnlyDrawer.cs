using UnityEditor;
using UnityEngine;

/// <summary>
/// Allows us to use the [ReadOnly] on MonoBehaviour fields to make them greyed-out and un-interactable. This
/// is useful for when we want to spy on a variable in the Editor, but we don't want the user to be able to
/// change it from the Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Save the original GUI state
        GUI.enabled = false;

        // Draw the property in a disabled state
        EditorGUI.PropertyField(position, property, label);

        // Restore the original GUI state
        GUI.enabled = true;
    }
}
