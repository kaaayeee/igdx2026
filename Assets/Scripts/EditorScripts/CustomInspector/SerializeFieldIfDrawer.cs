#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializeFieldIfAttribute))]
public class SerializeFieldIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializeFieldIfAttribute showIf = (SerializeFieldIfAttribute)attribute;
        SerializedProperty boolProp = FindRelativeProperty(property, showIf.BoolFieldName);

        if (boolProp == null)
        {
            // Bool field not found — show HelpBox warning, treat as visible (bool = false)
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
            EditorGUI.HelpBox(helpBoxRect,
                $"SerializeFieldIf: Bool field '{showIf.BoolFieldName}' not found.",
                MessageType.Warning);

            Rect propertyRect = new Rect(position.x, position.y + helpBoxHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUI.GetPropertyHeight(property, label, true));

            EditorGUI.PropertyField(propertyRect, property, label, true);
            return;
        }

        if (boolProp.propertyType != SerializedPropertyType.Boolean)
        {
            // Field exists but is not a bool — show HelpBox warning, treat as visible
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
            EditorGUI.HelpBox(helpBoxRect,
                $"SerializeFieldIf: Field '{showIf.BoolFieldName}' is not a bool.",
                MessageType.Warning);

            Rect propertyRect = new Rect(position.x, position.y + helpBoxHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUI.GetPropertyHeight(property, label, true));

            EditorGUI.PropertyField(propertyRect, property, label, true);
            return;
        }

        // Normal path: determine visibility
        bool boolValue = boolProp.boolValue;
        bool shouldHide = showIf.Invert ? boolValue : !boolValue;

        if (!shouldHide)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializeFieldIfAttribute showIf = (SerializeFieldIfAttribute)attribute;
        SerializedProperty boolProp = FindRelativeProperty(property, showIf.BoolFieldName);

        // Error cases: show HelpBox + property
        if (boolProp == null || boolProp.propertyType != SerializedPropertyType.Boolean)
        {
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            return helpBoxHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property, label, true);
        }

        // Normal path: 0 height when hidden, normal height when visible
        bool boolValue = boolProp.boolValue;
        bool shouldHide = showIf.Invert ? boolValue : !boolValue;

        if (shouldHide)
        {
            return 0f;
        }

        return EditorGUI.GetPropertyHeight(property, label, true);
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
