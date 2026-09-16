#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ohm.UISystem.Editor
{
    [CustomEditor(typeof(TransitionBase), true)]
    [CanEditMultipleObjects]
    public class TransitionBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show (Instant)"))
            {
                TriggerAll(isShow: true);
            }
            if (GUILayout.Button("Hide (Instant)"))
            {
                TriggerAll(isShow: false);
            }
            EditorGUILayout.EndHorizontal();

            if (!AnyTargetSupportsCapture()) return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Capture Current State", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture Show Config"))
            {
                Capture(isShow: true);
            }
            if (GUILayout.Button("Capture Hide Config"))
            {
                Capture(isShow: false);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void TriggerAll(bool isShow)
        {
            foreach (var obj in targets)
            {
                if (obj is not TransitionBase t) continue;

                if (isShow) t.TriggerShow(true);
                else t.TriggerHide(true);

                if (!Application.isPlaying) MarkDirty(t);
            }
        }

        private bool AnyTargetSupportsCapture()
        {
            foreach (var obj in targets)
            {
                if (obj is TransitionBase t && t.SupportsCapture) return true;
            }
            return false;
        }

        private void Capture(bool isShow)
        {
            string which = isShow ? "show" : "hide";
            if (!EditorUtility.DisplayDialog(
                    "Capture " + which + " config",
                    "Overwrite the " + which + " config with the object's current values?",
                    "Capture", "Cancel"))
            {
                return;
            }

            foreach (var obj in targets)
            {
                if (obj is not TransitionBase t || !t.SupportsCapture) continue;

                Undo.RecordObject(t, "Capture " + which + " config");
                if (isShow) t.CaptureShowConfig();
                else t.CaptureHideConfig();
                MarkDirty(t);
            }
        }

        private static void MarkDirty(TransitionBase transition)
        {
            EditorUtility.SetDirty(transition);
            if (!transition.gameObject.scene.IsValid()) return;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(transition.gameObject.scene);
        }
    }
}
#endif
