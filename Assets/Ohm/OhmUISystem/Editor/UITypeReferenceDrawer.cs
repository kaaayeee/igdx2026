using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ohm.UISystem
{
    [CustomPropertyDrawer(typeof(UITypeReference))]
    public class UITypeReferenceDrawer : PropertyDrawer
    {
        private Type[] uiTypes;
        private string[] uiTypeNames;

        private void Initialize()
        {
            if (uiTypes == null || uiTypeNames == null)
            {
    #if UNITY_2019_2_OR_NEWER
                // Use TypeCache for much faster lookup in newer Unity versions
                var derivedTypes = TypeCache.GetTypesDerivedFrom<UIBase>();
                uiTypes = derivedTypes.Where(p => !p.IsAbstract).ToArray();
    #else
                var baseType = typeof(UIBase);
                uiTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => baseType.IsAssignableFrom(p) && !p.IsAbstract)
                    .ToArray();
    #endif
                uiTypeNames = new string[uiTypes.Length + 1];
                uiTypeNames[0] = "NONE";
                for (int i = 0; i < uiTypes.Length; i++)
                {
                    uiTypeNames[i + 1] = uiTypes[i].Name;
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize();

            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty typeNameProp = property.FindPropertyRelative("typeName");

            int selectedIndex = 0;
            string currentTypeName = typeNameProp.stringValue;

            if (!string.IsNullOrEmpty(currentTypeName))
            {
                Type currentType = Type.GetType(currentTypeName);
                if (currentType != null)
                {
                    for (int i = 0; i < uiTypes.Length; i++)
                    {
                        if (uiTypes[i] == currentType)
                        {
                            selectedIndex = i + 1;
                            break;
                        }
                    }
                }
            }

            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, uiTypeNames);

            if (selectedIndex == 0)
            {
                typeNameProp.stringValue = "";
            }
            else
            {
                typeNameProp.stringValue = uiTypes[selectedIndex - 1].AssemblyQualifiedName;
            }

            EditorGUI.EndProperty();
        }
    }
}
