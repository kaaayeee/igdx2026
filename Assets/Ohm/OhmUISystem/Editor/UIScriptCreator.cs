using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Ohm.UISystem.Editor
{
    /// <summary>
    /// Adds "Create > OhmUI > UI Script" to the Project window, generating a UIBase subclass
    /// with the same inline-rename flow as Unity's built-in "Create > C# Script".
    /// </summary>
    public static class UIScriptCreator
    {
        private const string DEFAULT_FILE_NAME = "UINewScreen.cs";
        private const string PLACEHOLDER = "#SCRIPTNAME#";

        private const string TEMPLATE_BASIC =
@"using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;

public class #SCRIPTNAME# : UIBase
{
    //[Header(""References"")]
    // [SerializeField] private Button closeButton;

    // public override void Show(bool instant = false)
    // {
    //     base.Show(instant);
    // }

    // public override void Hide(bool instant = false)
    // {
    //     base.Hide(instant);
    // }
}
";

        private const string TEMPLATE_INJECTABLE =
@"using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;

[System.Serializable]
public struct #SCRIPTNAME#Data
{
    // public string title;
}

public class #SCRIPTNAME# : UIBase, IUIInjectable<#SCRIPTNAME#Data>
{
    //[Header(""References"")]
    // [SerializeField] private Button closeButton;

    public void Inject(#SCRIPTNAME#Data data)
    {
    }

    // public override void Show(bool instant = false)
    // {
    //     base.Show(instant);
    // }

    // public override void Hide(bool instant = false)
    // {
    //     base.Hide(instant);
    // }
}
";

        [MenuItem("Assets/Create/OhmUI/UI Script", false, 0)]
        private static void CreateUIScript() => CreateFromTemplate(TEMPLATE_BASIC);

        [MenuItem("Assets/Create/OhmUI/UI Script (Injectable)", false, 1)]
        private static void CreateInjectableUIScript() => CreateFromTemplate(TEMPLATE_INJECTABLE);

        private static void CreateFromTemplate(string template)
        {
            var action = ScriptableObject.CreateInstance<DoCreateUIScript>();
            action.template = template;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                default(EntityId),
                action,
                DEFAULT_FILE_NAME,
                EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D,
                null);
        }

        private class DoCreateUIScript : AssetCreationEndAction
        {
            public string template;

            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                string className = SanitizeName(Path.GetFileNameWithoutExtension(pathName));
                if (string.IsNullOrEmpty(className))
                {
                    Debug.LogError($"OhmUI: '{Path.GetFileNameWithoutExtension(pathName)}' cannot be turned into a valid C# class name.");
                    return;
                }

                if (ScriptNameExists(className))
                {
                    Debug.LogWarning($"OhmUI: A script named '{className}' already exists; Unity will report a duplicate type error until one of them is renamed.");
                }

                string content = template.Replace(PLACEHOLDER, className);
                File.WriteAllText(Path.GetFullPath(pathName), content, new UTF8Encoding(true));

                AssetDatabase.ImportAsset(pathName);
                ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(pathName));
            }
        }

        private static bool ScriptNameExists(string className)
        {
            return AssetDatabase.FindAssets($"{className} t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Any(path => Path.GetFileNameWithoutExtension(path) == className);
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            // Remove invalid characters — keep letters, digits, underscores only
            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            // Ensure it doesn't start with a digit
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return sanitized;
        }
    }
}
