using UnityEngine;

public class ReadOnlyIfAttribute : PropertyAttribute
{
    public string BoolFieldName;
    public bool Invert;

    /// <summary>
    /// Makes this field read-only based on a boolean field's value.
    /// Default (invert=false): read-only when the bool is TRUE, editable when FALSE.
    /// With invert=true: read-only when the bool is FALSE, editable when TRUE.
    /// </summary>
    public ReadOnlyIfAttribute(string boolFieldName, bool invert = false)
    {
        BoolFieldName = boolFieldName;
        Invert = invert;
    }
}
