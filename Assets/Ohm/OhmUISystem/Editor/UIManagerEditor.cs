using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
namespace Ohm.UISystem
{
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 'dontDestroyOnLoad' comes from SingletonMonoBehaviour and is dead here — the bootstrap
            // calls DontDestroyOnLoad itself. It can't be removed from the shared base, only hidden.
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "dontDestroyOnLoad");
            serializedObject.ApplyModifiedProperties();

            UIManager script = (UIManager)target;

            // --- Warnings ---
            // Only a real scene instance is a mistake — the prefab asset and Prefab Stage are fine.
            bool livesInScene = !EditorUtility.IsPersistent(script) &&
                                UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(script.gameObject) == null;

            if (!Application.isPlaying && livesInScene)
            {
                EditorGUILayout.HelpBox(
                    "This UIManager lives in a scene. The global UIManager is created automatically before the first " +
                    "scene loads, so this one will be destroyed at runtime and its UI list ignored.\n\n" +
                    "Remove it: put project-wide UIs in Project Settings > Ohm UI > UI Settings, and register UIs that " +
                    "need scene references with a UIBakedHandler.",
                    MessageType.Error);
            }

            // --- Runtime Debug Info ---
            if (Application.isPlaying)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Runtime Layer State", EditorStyles.boldLabel);

                var layers = (UILayer[])Enum.GetValues(typeof(UILayer));
                foreach (var layer in layers.OrderBy(l => (int)l))
                {
                    Type activeUI = script.GetCurrentUI(layer);
                    int historyCount = script.GetHistoryCount(layer);

                    string uiName = activeUI != null ? activeUI.Name : "(empty)";
                    string historyInfo = historyCount > 0 ? $" [history: {historyCount}]" : "";

                    EditorGUILayout.LabelField(
                        $"  Layer {(int)layer} [{layer}]",
                        $"{uiName}{historyInfo}");
                }

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Registered UIs", EditorStyles.boldLabel);

                foreach (var type in script.RegisteredUITypes.OrderBy(t => t.Name))
                {
                    EditorGUILayout.LabelField(
                        $"  {type.Name}",
                        script.IsSceneRegistered(type) ? "scene (baked)" : "project default");
                }

                EditorGUILayout.Space(5);
                Repaint(); // Keep refreshing during play mode
            }
        }
    }
}
#endif
