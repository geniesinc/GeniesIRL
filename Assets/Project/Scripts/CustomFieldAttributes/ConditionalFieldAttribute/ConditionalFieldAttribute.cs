using UnityEngine;

namespace GeniesIRL
{
    /// <summary>
    /// Allows us to easily mark fields as conditional in the Inspector. For example, if you want an Inspector
    /// property to only be visible if a box is ticked, you can mark the property with [ConditionalField("myBoolField")]
    /// </summary>
    public class ConditionalFieldAttribute : PropertyAttribute
    {
        public string ConditionalSourceField;

        public ConditionalFieldAttribute(string conditionalSourceField)
        {
            ConditionalSourceField = conditionalSourceField;
        }
    }
}

