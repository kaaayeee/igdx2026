#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ohm.UISystem.Editor
{
    /// <summary>Project Settings > Ohm UI > UI Settings — picks the active UI config and edits the project's layer list.</summary>
    public class OhmUISettingsProvider : SettingsProvider
    {
        private const string RootFolder = "Assets/Ohm/OhmUISystem";
        private const string ResourcesFolder = RootFolder + "/Resources";
        private const string CanvasPrefabPath = RootFolder + "/Prefabs/UICanvas.prefab";
        private const string DefaultConfigPath = ResourcesFolder + "/OhmUISettings.asset";

        private SerializedObject layerObject;
        private SerializedProperty layerNamesProperty;

        public OhmUISettingsProvider(string path, SettingsScope scope) : base(path, scope) {}

        #region Asset Creation

        // The locator must exist before anyone opens this page — the runtime loads it from Resources and
        // cannot create it. delayCall keeps it off the first import pass.
        [InitializeOnLoadMethod]
        private static void EnsureAssetsExist() => EditorApplication.delayCall += () => GetOrCreateLocator();

        /// <summary>Returns the fixed-path locator, creating it (and a first config) if the project has none.</summary>
        public static OhmUISettingsLocator GetOrCreateLocator()
        {
            var locator = AssetDatabase.LoadAssetAtPath<OhmUISettingsLocator>(OhmUISettingsLocator.AssetPath);

            if (locator == null)
            {
                if (!AssetDatabase.IsValidFolder(RootFolder)) return null;
                if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                    AssetDatabase.CreateFolder(RootFolder, "Resources");

                locator = ScriptableObject.CreateInstance<OhmUISettingsLocator>();
                AssetDatabase.CreateAsset(locator, OhmUISettingsLocator.AssetPath);
            }

            if (locator.active == null)
            {
                locator.active = AssetDatabase.LoadAssetAtPath<OhmUISettings>(DefaultConfigPath)
                                 ?? CreateConfig(DefaultConfigPath);
                EditorUtility.SetDirty(locator);
                AssetDatabase.SaveAssets();
            }

            return locator;
        }

        private static OhmUISettings CreateConfig(string path)
        {
            var config = ScriptableObject.CreateInstance<OhmUISettings>();
            config.canvasPrefab = AssetDatabase.LoadAssetAtPath<Canvas>(CanvasPrefabPath);
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            return config;
        }

        #endregion

        #region Provider Lifecycle

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            layerObject = new SerializedObject(UILayerConfig.instance);
            layerNamesProperty = layerObject.FindProperty("layerNames");
        }

        public override void OnDeactivate()
        {
            PromptForUnsavedLayers();
            base.OnDeactivate();
        }

        public override void OnGUI(string searchContext)
        {
            DrawActiveConfig();
            EditorGUILayout.Space(16);
            DrawLayers();
        }

        [SettingsProvider]
        public static SettingsProvider CreateOhmUISettingsProvider()
        {
            return new OhmUISettingsProvider("Project/Ohm UI/UI Settings", SettingsScope.Project)
            {
                keywords = new[] { "Ohm", "UI", "UIManager", "Bootstrap", "Config", "Prefab", "Start", "Layer" }
            };
        }

        #endregion

        #region UI Config

        private void DrawActiveConfig()
        {
            EditorGUILayout.LabelField("UI Config", EditorStyles.boldLabel);

            var locator = GetOrCreateLocator();
            if (locator == null)
            {
                EditorGUILayout.HelpBox($"Could not create the settings locator — '{RootFolder}' does not exist.", MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "The global UIManager is created automatically before the first scene loads, from the config below.\n\n" +
                "Keep as many configs as you like (dev, mobile, demo) and switch the active one here — only the active " +
                "one runs and ships. Select the asset to edit its UI list and options.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            var active = (OhmUISettings)EditorGUILayout.ObjectField("Active Config", locator.active, typeof(OhmUISettings), false);
            if (EditorGUI.EndChangeCheck())
            {
                locator.active = active;
                EditorUtility.SetDirty(locator);
                AssetDatabase.SaveAssetIfDirty(locator);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create New Config..."))
                    CreateNewConfig(locator);

                using (new EditorGUI.DisabledScope(locator.active == null))
                {
                    if (GUILayout.Button("Select Config"))
                    {
                        Selection.activeObject = locator.active;
                        EditorGUIUtility.PingObject(locator.active);
                    }
                }
            }

            if (locator.active == null)
            {
                EditorGUILayout.HelpBox(
                    "No active config — no UIManager will be created at runtime. Assign one above, or create a new one.",
                    MessageType.Error);
            }
        }

        private void CreateNewConfig(OhmUISettingsLocator locator)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Ohm UI Config", "OhmUISettings", "asset", "Where should the new UI config be saved?");
            if (string.IsNullOrEmpty(path)) return;

            locator.active = CreateConfig(path);
            EditorUtility.SetDirty(locator);
            AssetDatabase.SaveAssets();
            GUIUtility.ExitGUI(); // the file panel invalidated this layout pass
        }

        #endregion

        #region Layers

        private void DrawLayers()
        {
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);

            // Don't auto-update if we have pending changes, so we don't overwrite user edits
            if (!layerObject.hasModifiedProperties)
                layerObject.UpdateIfRequiredOrScript();

            EditorGUILayout.HelpBox(
                "Add, remove, or reorder layers using the list below. " +
                "Index 0 = lowest priority (background), last = highest priority (overlay).\n\n" +
                "Layers are project-wide: they generate the UILayer enum at compile time, so every config shares them.",
                MessageType.Info);

            EditorGUILayout.PropertyField(layerNamesProperty, true);

            GUILayout.Space(10);

            if (layerObject.hasModifiedProperties)
            {
                EditorGUILayout.HelpBox("There are unsaved layer changes. Click 'Save & Regenerate' to apply, or 'Discard Changes' to revert.", MessageType.Warning);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save & Regenerate", GUILayout.Height(30)))
                    SaveLayers();

                if (GUILayout.Button("Discard Changes", GUILayout.Height(30)))
                    layerObject.Update(); // pull data back from the actual instance
                GUILayout.EndHorizontal();
            }
            else if (GUILayout.Button("Force Regenerate UILayer Enum", GUILayout.Height(30)))
            {
                UILayerGenerator.GenerateFromConfig(UILayerConfig.instance);
            }
        }

        private void SaveLayers()
        {
            layerObject.ApplyModifiedProperties();
            UILayerConfig.instance.SaveConfig();
            UILayerGenerator.GenerateFromConfig(UILayerConfig.instance);
        }

        private void PromptForUnsavedLayers()
        {
            if (layerObject == null || !layerObject.hasModifiedProperties) return;

            if (EditorUtility.DisplayDialog(
                "Unsaved Changes",
                "You have unsaved changes in your Ohm UI layer list. Do you want to save and regenerate the UILayer enum before leaving?",
                "Save & Regenerate",
                "Discard"))
            {
                SaveLayers();
            }
            else
            {
                layerObject.Update();
            }
        }

        #endregion
    }
}
#endif
