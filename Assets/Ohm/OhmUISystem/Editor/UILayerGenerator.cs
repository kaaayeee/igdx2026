using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Ohm.UISystem
{
    public static class UILayerGenerator
    {
        private const string FILE_PATH = "Assets/Ohm/OhmUISystem/Runtime/UITypeReference/UILayer.g.cs";

        [DidReloadScripts]
        public static void OnScriptsReloaded()
        {
            var config = FindLayerConfig();
            if (config != null)
            {
                GenerateFromConfig(config);
            }
            else
            {
                GenerateDefault();
            }
        }

        public static void GenerateFromConfig(UILayerConfig config)
        {
            if (config == null || config.layerNames == null || config.layerNames.Count == 0)
            {
                GenerateDefault();
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED FILE. DO NOT MODIFY.");
            sb.AppendLine();
            sb.AppendLine("namespace Ohm.UISystem");
            sb.AppendLine("{");
            sb.AppendLine("    public enum UILayer");
            sb.AppendLine("    {");

            var usedNames = new System.Collections.Generic.HashSet<string>();

            for (int i = 0; i < config.layerNames.Count; i++)
            {
                string sanitized = SanitizeName(config.layerNames[i]);
                if (string.IsNullOrEmpty(sanitized))
                {
                    Debug.LogWarning($"OhmUI Layer Generator: Layer name at index {i} is empty or invalid. Skipping.");
                    continue;
                }

                if (!usedNames.Add(sanitized))
                {
                    Debug.LogWarning($"OhmUI Layer Generator: Duplicate layer name '{sanitized}' at index {i}. Skipping.");
                    continue;
                }

                sb.AppendLine($"        {sanitized} = {i},");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            WriteIfChanged(sb.ToString());
        }

        private static void GenerateDefault()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED FILE. DO NOT MODIFY.");
            sb.AppendLine();
            sb.AppendLine("namespace Ohm.UISystem");
            sb.AppendLine("{");
            sb.AppendLine("    public enum UILayer");
            sb.AppendLine("    {");
            sb.AppendLine("        Main = 0,");
            sb.AppendLine("        Popup = 1,");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            WriteIfChanged(sb.ToString());
        }

        private static void WriteIfChanged(string newContent)
        {
            bool shouldWrite = true;

            if (File.Exists(FILE_PATH))
            {
                string existingContent = File.ReadAllText(FILE_PATH);
                if (existingContent == newContent)
                {
                    shouldWrite = false;
                }
            }

            if (shouldWrite)
            {
                string dir = Path.GetDirectoryName(FILE_PATH);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(FILE_PATH, newContent);
                AssetDatabase.ImportAsset(FILE_PATH, ImportAssetOptions.ForceUpdate);
            }
        }

        public static UILayerConfig FindLayerConfig()
        {
            return UILayerConfig.instance;
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            // Remove invalid characters â€” keep letters, digits, underscores only
            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            // Ensure it doesn't start with a digit
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return sanitized;
        }
    }
}
