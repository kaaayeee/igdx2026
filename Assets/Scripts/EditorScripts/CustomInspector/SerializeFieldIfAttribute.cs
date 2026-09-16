using UnityEngine;

public class SerializeFieldIfAttribute : PropertyAttribute
{
    public string BoolFieldName;
    public bool Invert;

    /// <summary>
    /// Shows or hides this field in the inspector based on a boolean field's value.
    /// Default (invert=false): visible when the bool is TRUE, hidden when FALSE.
    /// With invert=true: visible when the bool is FALSE, hidden when TRUE.
    /// </summary>
    public SerializeFieldIfAttribute(string boolFieldName, bool invert = false)
    {
        BoolFieldName = boolFieldName;
        Invert = invert;
    }
}
