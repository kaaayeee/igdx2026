using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Ohm.UISystem
{
    public static class UITypeGenerator
    {
        // Path untuk menyimpan file auto-generated (bisa disesuaikan jika ingin di folder lain)
        private const string FILE_PATH = "Assets/Ohm/OhmUISystem/Runtime/UITypeReference/UIType.g.cs";

        // Screen types whose scripts are about to be deleted. UIType.g.cs must stop referencing them
        // BEFORE Unity recompiles, otherwise the generated typeof() fails to compile and
        // [DidReloadScripts] never fires to repair it — a deadlock only a hand-edit can break.
        private static readonly HashSet<Type> pendingDeletions = new HashSet<Type>();

        // Atribut ini membuat method dipanggil otomatis setiap kali kompilasi script Unity selesai
        [DidReloadScripts]
        public static void GenerateUITypeClass()
        {
            // A reload wipes these statics anyway; clearing keeps the intent explicit.
            pendingDeletions.Clear();
            Generate(importImmediately: true);
        }

        /// <summary>Escape hatch for stale entries the delete hook can't see (scripts removed outside Unity, a class renamed in-file).</summary>
        [MenuItem("Tools/Ohm/Regenerate UIType Class")]
        private static void ForceRegenerate()
        {
            pendingDeletions.Clear();
            Generate(importImmediately: true);
        }

        private static void Generate(bool importImmediately)
        {
            // Mencari semua class yang mewarisi UIBase dan bukan class abstract
            var derivedTypes = TypeCache.GetTypesDerivedFrom<UIBase>()
                .Where(t => !t.IsAbstract && !pendingDeletions.Contains(t))
                .OrderBy(t => t.Name)
                .ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED FILE. DO NOT MODIFY.");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace Ohm.UISystem");
            sb.AppendLine("{");
            sb.AppendLine("    public static class UIType");
            sb.AppendLine("    {");

            foreach (var type in derivedTypes)
            {
                // Mengatasi kasus nested class menggunakan Replace "+" menjadi "."
                string fullTypeName = type.FullName.Replace("+", ".");
                sb.AppendLine($"        public static readonly Type {type.Name} = typeof({fullTypeName});");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            string newContent = sb.ToString();

            // Hanya tulis/replace file jika kontennya berubah (untuk menghindari infinite compile loop)
            if (File.Exists(FILE_PATH) && File.ReadAllText(FILE_PATH) == newContent) return;

            // Pastikan direktori ada
            string dir = Path.GetDirectoryName(FILE_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(FILE_PATH, newContent);

            // Memberitahu Unity bahwa ada asset baru agar langsung ter-refresh dan terbaca oleh IDE.
            // Dilewati saat dipanggil dari OnWillDeleteAsset — memicu import di tengah operasi delete
            // tidak aman, dan refresh yang mengikuti delete sudah otomatis membaca file yang berubah.
            if (importImmediately) AssetDatabase.ImportAsset(FILE_PATH, ImportAssetOptions.ForceUpdate);
        }

        // --- Deletion Pruning ---

        internal static void NotifyPendingDeletion(string assetPath)
        {
            var doomed = CollectScreenTypes(assetPath);
            if (doomed.Count == 0) return;

            foreach (var type in doomed) pendingDeletions.Add(type);

            Generate(importImmediately: false);
            Debug.Log($"OhmUI: removed {doomed.Count} deleted screen type(s) from UIType — {string.Join(", ", doomed.Select(t => t.Name))}.");
        }

        private static List<Type> CollectScreenTypes(string assetPath)
        {
            var result = new List<Type>();

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { assetPath }))
                    AddIfScreenType(AssetDatabase.GUIDToAssetPath(guid), result);
                return result;
            }

            AddIfScreenType(assetPath, result);
            return result;
        }

        private static void AddIfScreenType(string path, List<Type> result)
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return;

            var cls = AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass();
            if (cls == null || cls.IsAbstract || cls == typeof(UIBase)) return;
            if (!typeof(UIBase).IsAssignableFrom(cls)) return;

            result.Add(cls);
        }
    }

    /// <summary>Prunes UIType.g.cs before a deleted screen script triggers the recompile that would break it.</summary>
    internal class UIScreenScriptDeletionWatcher : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            UITypeGenerator.NotifyPendingDeletion(assetPath);
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
