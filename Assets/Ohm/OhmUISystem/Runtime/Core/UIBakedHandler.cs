using System.Collections.Generic;
using UnityEngine;

namespace Ohm.UISystem
{
    /// <summary>
    /// Registers UI instances placed in this scene with the global UIManager, so a UI can hold hard
    /// references to scene objects. They are unregistered and destroyed when the scene unloads.
    /// </summary>
    public class UIBakedHandler : MonoBehaviour
    {
        [Header("Baked UIs")]
        [Tooltip("UI objects placed in THIS scene. Drag scene instances here, not prefabs — a prefab cannot reference scene objects, and project-wide UIs belong in Project Settings > Ohm UI > UI Settings.")]
        [SerializeField] private List<UIBase> bakedUIs = new();

        public IReadOnlyList<UIBase> BakedUIs => bakedUIs;

        private void Awake()
        {
            // The UIManager bootstrap runs BeforeSceneLoad, so Instance is live by any scene Awake.
            if (UIManager.Instance == null)
            {
                Debug.LogWarning($"OhmUI: UIBakedHandler on '{name}' found no UIManager. Check that Auto Bootstrap is on in Project Settings > Ohm UI > UI Settings.");
                return;
            }

            foreach (var ui in bakedUIs)
            {
                if (ui != null)
                    UIManager.Instance.RegisterInstance(ui);
            }
        }

        private void OnDestroy()
        {
            // Teardown order is undefined on application quit — the manager may already be gone.
            if (UIManager.Instance == null) return;

            foreach (var ui in bakedUIs)
            {
                if (ui != null)
                    UIManager.Instance.UnregisterInstance(ui);
            }
        }
    }
}
