#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohm.UISystem.Editor
{
    [CustomEditor(typeof(UIBakedHandler))]
    public class UIBakedHandlerEditor : UnityEditor.Editor
    {
        private UIBase prefabToBake;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var handler = (UIBakedHandler)target;

            EditorGUILayout.Space(8);
            DrawValidation(handler);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Bake A UI Into This Scene", EditorStyles.boldLabel);

            prefabToBake = (UIBase)EditorGUILayout.ObjectField("UI Prefab", prefabToBake, typeof(UIBase), false);

            using (new EditorGUI.DisabledScope(prefabToBake == null))
            {
                if (GUILayout.Button("Bake Into Scene", GUILayout.Height(28)))
                    BakeIntoScene(handler, prefabToBake);
            }
        }

        private void DrawValidation(UIBakedHandler handler)
        {
            var seenTypes = new HashSet<Type>();

            foreach (var ui in handler.BakedUIs)
            {
                if (ui == null) continue;

                if (PrefabUtility.IsPartOfPrefabAsset(ui))
                {
                    EditorGUILayout.HelpBox(
                        $"'{ui.name}' is a prefab asset, not a scene object. Only UIs placed in this scene belong here — " +
                        "project-wide UIs go in Project Settings > Ohm UI > UI Settings. Use the button below to bake one in.",
                        MessageType.Error);
                    continue;
                }

                if (!seenTypes.Add(ui.GetType()))
                {
                    EditorGUILayout.HelpBox(
                        $"More than one '{ui.GetType().Name}' is listed. One UI type maps to one entry — only the first is registered.",
                        MessageType.Error);
                }

                if (ui.Detached)
                {
                    EditorGUILayout.HelpBox(
                        $"'{ui.name}' has Detached ticked and will be rejected at runtime — a scene instance cannot be pooled or cloned. " +
                        "Untick Detached, or move this UI into the project's UI Settings config.",
                        MessageType.Error);
                }
            }

            DrawScalerMismatchWarning(handler);
        }

        /// <summary>Registered instances are re-parented under the global canvas at runtime, so a scaler mismatch moves the layout between edit and play mode.</summary>
        private void DrawScalerMismatchWarning(UIBakedHandler handler)
        {
            var sceneScaler = handler.GetComponentInParent<CanvasScaler>();
            if (sceneScaler == null) return;

            // Read-only lookup — never create assets from an inspector paint.
            var settings = OhmUISettings.Instance;
            if (settings == null) return;

            // With no Canvas Prefab assigned the manager builds one in code — compare against those
            // defaults rather than skipping the check.
            var globalScaler = settings.canvasPrefab != null ? settings.canvasPrefab.GetComponent<CanvasScaler>() : null;

            var globalMode = globalScaler != null ? globalScaler.uiScaleMode : UIManager.DefaultScaleMode;
            var globalResolution = globalScaler != null ? globalScaler.referenceResolution : UIManager.DefaultReferenceResolution;

            if (globalMode == sceneScaler.uiScaleMode && globalResolution == sceneScaler.referenceResolution)
                return;

            string globalName = settings.canvasPrefab != null ? $"'{settings.canvasPrefab.name}'" : "the built-in canvas";

            EditorGUILayout.HelpBox(
                $"This scene's CanvasScaler ({sceneScaler.uiScaleMode}, {sceneScaler.referenceResolution}) does not match " +
                $"{globalName} ({globalMode}, {globalResolution}). Baked UIs are re-parented under the global canvas at " +
                "runtime, so the layout will shift between edit mode and play mode.",
                MessageType.Warning);
        }

        private void BakeIntoScene(UIBakedHandler handler, UIBase prefab)
        {
            if (handler.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogWarning($"OhmUI: '{handler.name}' is not under a Canvas — the baked UI will not lay out correctly while authoring. Put the UIBakedHandler on (or under) a Canvas.");
            }

            var instance = (UIBase)PrefabUtility.InstantiatePrefab(prefab, handler.transform);
            instance.name = prefab.name;
            Undo.RegisterCreatedObjectUndo(instance.gameObject, "Bake UI Into Scene");

            var list = serializedObject.FindProperty("bakedUIs");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = instance;
            serializedObject.ApplyModifiedProperties();

            prefabToBake = null;
            Selection.activeGameObject = instance.gameObject;
        }
    }
}
#endif
