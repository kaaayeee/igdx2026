#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyIfAttribute))]
public class ReadOnlyIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ReadOnlyIfAttribute readOnlyIf = (ReadOnlyIfAttribute)attribute;
        SerializedProperty boolProp = FindRelativeProperty(property, readOnlyIf.BoolFieldName);

        bool isReadOnly = false;

        if (boolProp == null)
        {
            // Bool field not found — show HelpBox warning, treat as editable (bool = false)
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
            EditorGUI.HelpBox(helpBoxRect,
                $"ReadOnlyIf: Bool field '{readOnlyIf.BoolFieldName}' not found.",
                MessageType.Warning);

            Rect propertyRect = new Rect(position.x, position.y + helpBoxHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUI.GetPropertyHeight(property, label, true));

            // Treat missing bool as false → apply invert logic
            isReadOnly = readOnlyIf.Invert; // false ^ invert

            GUI.enabled = !isReadOnly;
            EditorGUI.PropertyField(propertyRect, property, label, true);
            GUI.enabled = true;
        }
        else if (boolProp.propertyType != SerializedPropertyType.Boolean)
        {
            // Field exists but is not a bool — show HelpBox warning, treat as editable
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
            EditorGUI.HelpBox(helpBoxRect,
                $"ReadOnlyIf: Field '{readOnlyIf.BoolFieldName}' is not a bool.",
                MessageType.Warning);

            Rect propertyRect = new Rect(position.x, position.y + helpBoxHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUI.GetPropertyHeight(property, label, true));

            GUI.enabled = true;
            EditorGUI.PropertyField(propertyRect, property, label, true);
        }
        else
        {
            // Normal path: bool field found
            bool boolValue = boolProp.boolValue;
            isReadOnly = readOnlyIf.Invert ? !boolValue : boolValue;

            GUI.enabled = !isReadOnly;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ReadOnlyIfAttribute readOnlyIf = (ReadOnlyIfAttribute)attribute;
        SerializedProperty boolProp = FindRelativeProperty(property, readOnlyIf.BoolFieldName);

        float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);

        // Add extra height for HelpBox when the bool field is missing or not a bool
        if (boolProp == null ||  boolProp.propertyType != SerializedPropertyType.Boolean)
        {
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            return helpBoxHeight + EditorGUIUtility.standardVerticalSpacing + propertyHeight;
        }

        return propertyHeight;
    }

    private SerializedProperty FindRelativeProperty(SerializedProperty property, string propertyName)
    {
        string path = property.propertyPath;
        int lastDot = path.LastIndexOf('.');
        if (lastDot == -1)
        {
            // Root property, just find it directly
            return property.serializedObject.FindProperty(propertyName);
        }
        else
        {
            // Nested property, build relative path
            string parentPath = path.Substring(0, lastDot);
            return property.serializedObject.FindProperty(parentPath + "." + propertyName);
        }
    }
}
#endif
