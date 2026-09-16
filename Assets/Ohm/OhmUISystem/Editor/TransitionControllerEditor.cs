using UnityEditor;
using UnityEngine;

namespace Ohm.UISystem.Editor
{
    [CustomEditor(typeof(TransitionController))]
    public class TransitionControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TransitionController controller = (TransitionController)target;

            GUILayout.Space(10);
            
            // Membuat tombol yang lebar dan nyaman diklik
            if (GUILayout.Button("Fetch Transitions (Include Children)", GUILayout.Height(30)))
            {
                // Best practice: Merekam history Undo agar aksi ini bisa di-Ctrl+Z
                Undo.RecordObject(controller, "Fetch Transitions");
                
                controller.FetchTransitions();

                // Best practice: Menandai object sebagai 'kotor' agar Unity tahu perlu di-save
                EditorUtility.SetDirty(controller);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Capture Current State (All Transitions)", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture All Show Configs", GUILayout.Height(24)))
            {
                CaptureAll(controller, isShow: true);
            }
            if (GUILayout.Button("Capture All Hide Configs", GUILayout.Height(24)))
            {
                CaptureAll(controller, isShow: false);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Show (Instant)"))
            {
                controller.Show(instant: true);
                if (!Application.isPlaying) MarkDirty(controller);
            }
            if (GUILayout.Button("Hide (Instant)"))
            {
                controller.Hide(instant: true);
                if (!Application.isPlaying) MarkDirty(controller);
            }
            GUILayout.EndHorizontal();
        }

        private static void MarkDirty(TransitionController controller)
        {
            EditorUtility.SetDirty(controller);
            if (!controller.gameObject.scene.IsValid()) return;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        private static void CaptureAll(TransitionController controller, bool isShow)
        {
            string which = isShow ? "show" : "hide";
            if (!EditorUtility.DisplayDialog(
                    "Capture all " + which + " configs",
                    "Overwrite the " + which + " config of every transition in this controller with each " +
                    "object's current values?",
                    "Capture", "Cancel"))
            {
                return;
            }

            foreach (var setup in controller.Transitions)
            {
                if (setup.transition != null && setup.transition.SupportsCapture)
                    Undo.RecordObject(setup.transition, "Capture all " + which + " configs");
            }

            if (isShow) controller.CaptureAllShowConfigs();
            else controller.CaptureAllHideConfigs();

            foreach (var setup in controller.Transitions)
            {
                if (setup.transition == null || !setup.transition.SupportsCapture) continue;

                EditorUtility.SetDirty(setup.transition);
                var scene = setup.transition.gameObject.scene;
                if (scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
