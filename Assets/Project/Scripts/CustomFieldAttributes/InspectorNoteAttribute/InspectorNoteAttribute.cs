using UnityEngine;

namespace GeniesIRL 
{
    public class InspectorNoteAttribute : PropertyAttribute
    {
        public readonly string Text;

        public InspectorNoteAttribute(string text)
        {
            Text = text;
        }
    }
}

