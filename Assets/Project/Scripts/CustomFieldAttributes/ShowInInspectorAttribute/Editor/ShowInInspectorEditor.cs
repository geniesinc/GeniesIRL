using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace GeniesIRL 
{
    /// <summary>
    /// Attribute allows us to spy on non-serialized properties in the Inspector.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)] // Apply to all MonoBehaviour types
    public class ShowInInspectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default Inspector for serialized fields
            DrawDefaultInspector();

            // Only show dynamic properties in Play Mode
            if (Application.isPlaying)
            {
                // Get the target object
                var targetObject = target;

                // Use reflection to find properties with [ShowInInspector]
                var type = targetObject.GetType();
                var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var member in members)
                {
                    var attribute = member.GetCustomAttribute<ShowInInspectorAttribute>();
                    if (attribute != null)
                    {
                        if (member is PropertyInfo property && property.CanRead)
                        {
                            // Display the property value
                            var value = property.GetValue(targetObject);
                            EditorGUILayout.LabelField(property.Name, value?.ToString() ?? "null");
                        }
                    }
                }
            }
        }
    }
}