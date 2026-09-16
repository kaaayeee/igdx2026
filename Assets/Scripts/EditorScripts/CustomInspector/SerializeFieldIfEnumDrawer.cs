#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(SerializeFieldIfEnumAttribute))]
public class SerializeFieldIfEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializeFieldIfEnumAttribute attr = (SerializeFieldIfEnumAttribute)attribute;
        SerializedProperty enumProp = FindRelativeProperty(property, attr.EnumFieldName);

        if (enumProp == null)
        {
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
            EditorGUI.HelpBox(helpBoxRect,
                $"SerializeFieldIfEnum: Field '{attr.EnumFieldName}' not found.",
                MessageType.Warning);

            Rect propertyRect = new Rect(position.x, position.y + helpBoxHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUI.GetPropertyHeight(property, label, true));

            EditorGUI.PropertyField(propertyRect, property, label, true);
            return;
        }

        if (enumProp.propertyType != SerializedPropertyType.Enum)
        {
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
            EditorGUI.HelpBox(helpBoxRect,
                $"SerializeFieldIfEnum: Field '{attr.EnumFieldName}' is not an enum.",
                MessageType.Warning);

            Rect propertyRect = new Rect(position.x, position.y + helpBoxHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUI.GetPropertyHeight(property, label, true));

            EditorGUI.PropertyField(propertyRect, property, label, true);
            return;
        }

        // Normal path: show only when the enum matches one of the visible values
        int currentValue = enumProp.enumValueIndex;
        bool shouldShow = attr.VisibleValues.Contains(currentValue);

        if (shouldShow)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializeFieldIfEnumAttribute attr = (SerializeFieldIfEnumAttribute)attribute;
        SerializedProperty enumProp = FindRelativeProperty(property, attr.EnumFieldName);

        // Error cases: show HelpBox + property
        if (enumProp == null || enumProp.propertyType != SerializedPropertyType.Enum)
        {
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            return helpBoxHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property, label, true);
        }

        // Normal path: 0 height when hidden, normal height when visible
        int currentValue = enumProp.enumValueIndex;
        bool shouldShow = attr.VisibleValues.Contains(currentValue);

        if (!shouldShow)
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
            return property.serializedObject.FindProperty(propertyName);
        }
        else
        {
            string parentPath = path.Substring(0, lastDot);
            return property.serializedObject.FindProperty(parentPath + "." + propertyName);
        }
    }
}
#endif
