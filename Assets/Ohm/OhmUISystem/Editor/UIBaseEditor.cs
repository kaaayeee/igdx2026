#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ohm.UISystem.Editor
{
    [CustomEditor(typeof(UIBase), true)]
    [CanEditMultipleObjects]
    public class UIBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length > 1) return;

            var uiBase = (UIBase)target;
            EditorGUILayout.Space(8);

            if (uiBase.Detached)
            {
                string msg =
                    "Detached UI — excluded from history, back navigation, and auto-hide when a lower layer opens. " +
                    "You must hide it manually (instance.Hide() or UIManager.CloseUI(instance)); it is only auto-closed " +
                    "by CloseLayer / CloseAllUI.";
                if (uiBase.Pooled)
                {
                    msg += $" Pooled: multiple instances are recycled through an object pool — each ShowUI returns a fresh instance. " +
                           $"{uiBase.PoolSize} instance(s) are prewarmed when Spawn Behavior is PrewarmOnAwake; ";
                    msg += uiBase.DynamicPooling
                        ? "Dynamic Pooling is on, so the pool may instantiate past Pool Size on demand and keeps the extras."
                        : "Dynamic Pooling is off, so Pool Size is a hard cap — a show past it recycles the oldest showing instance.";
                }
                EditorGUILayout.HelpBox(msg, MessageType.Info);
            }
            else if (uiBase.Pooled)
            {
                EditorGUILayout.HelpBox(
                    "'Pooled' is ticked but only takes effect when 'Detached' is on — it is ignored and locked until then. " +
                    "Enable Detached to allow multiple pooled instances.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show (Instant)"))
            {
                uiBase.Show(instant: true);
                if (!Application.isPlaying) MarkDirty(uiBase);
            }
            if (GUILayout.Button("Hide (Instant)"))
            {
                uiBase.Hide(instant: true);
                if (!Application.isPlaying) MarkDirty(uiBase);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void MarkDirty(UIBase uiBase)
        {
            EditorUtility.SetDirty(uiBase);
            if (!uiBase.gameObject.scene.IsValid()) return;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiBase.gameObject.scene);
        }
    }
}
#endif
