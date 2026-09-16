#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ohm.UISystem.Editor
{
    /// <summary>
    /// One-shot cleanup after a navigation refactor. Removed components (UINavButtonList and,
    /// before it, UINavBindings / UINavButton) leave "missing script" entries on screen prefabs;
    /// this strips them. Type-independent — it removes any missing-script MonoBehaviour.
    /// </summary>
    public static class OhmLegacyCleanup
    {
        [MenuItem("Tools/Ohm/Strip Missing Nav Scripts (All Screen Prefabs)")]
        private static void StripMissingScripts()
        {
            if (!EditorUtility.DisplayDialog("Strip Missing Scripts",
                "Scan every prefab with a root UIBase and remove any components whose script is missing " +
                "(e.g. the old UINavButtonList). This edits the prefab assets. Proceed?",
                "Strip", "Cancel"))
                return;

            int prefabsChanged = 0;
            int componentsRemoved = 0;

            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("Strip Missing Scripts", path, (float)i / guids.Length);

                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null || go.GetComponent<UIBase>() == null) continue;

                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        int removed = 0;
                        foreach (var t in root.GetComponentsInChildren<Transform>(true))
                            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

                        if (removed > 0)
                        {
                            PrefabUtility.SaveAsPrefabAsset(root, path);
                            prefabsChanged++;
                            componentsRemoved += removed;
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Strip Missing Scripts",
                $"Removed {componentsRemoved} missing-script component(s) from {prefabsChanged} prefab(s).", "OK");
        }
    }
}
#endif
