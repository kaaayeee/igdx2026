using UnityEngine;

public class SerializeFieldIfEnumAttribute : PropertyAttribute
{
    public string EnumFieldName;
    public int[] VisibleValues;

    /// <summary>
    /// Shows or hides this field in the inspector based on an enum field's value.
    /// The field is only visible when the enum equals one of the specified values.
    /// Usage: [SerializeFieldIfEnum("targetMode", (int)TargetMode.Transform)]
    /// Multiple values: [SerializeFieldIfEnum("targetMode", (int)TargetMode.Transform, (int)TargetMode.Vector3)]
    /// </summary>
    public SerializeFieldIfEnumAttribute(string enumFieldName, params int[] visibleValues)
    {
        EnumFieldName = enumFieldName;
        VisibleValues = visibleValues;
    }
}
