using System.Collections.Generic;
using UnityEngine;

namespace Ohm.UISystem
{
    /// <summary>
    /// One UI configuration. A project can hold several (dev, mobile, demo); the active one is chosen
    /// in Project Settings > Ohm UI > UI Settings and reached at runtime via OhmUISettingsLocator.
    /// </summary>
    [CreateAssetMenu(fileName = "OhmUISettings", menuName = "OhmUI/UI Settings")]
    public class OhmUISettings : ScriptableObject
    {
        [Header("Bootstrap")]
        [Tooltip("Create the global UIManager automatically before the first scene loads, so no scene needs any UI setup. Turn off to place and manage a UIManager yourself.")]
        public bool autoBootstrap = true;

        [Tooltip("Canvas prefab the UI is built under. Edit or replace it freely — it is yours. Leave empty to get a built-in Screen Space - Overlay canvas scaled to 1920x1080.")]
        public Canvas canvasPrefab;

        [Header("UIs")]
        [Tooltip("UI prefabs available in every scene. UIs that must reference scene objects go on a UIBakedHandler in that scene instead.")]
        public List<UIBase> defaultUIPrefabs = new();

        [Tooltip("UI opened automatically once the global UIManager initializes (optional). Pick from the list above.")]
        public UITypeReference startUI;

        [Header("Scene Changes")]
        [Tooltip("Close every UI when a new scene loads. Off keeps HUDs and loading screens alive across the load.")]
        public bool closeAllOnSceneChange = false;

        private static OhmUISettingsLocator locator;

        /// <summary>The active UI config, or null when none is assigned in Project Settings > Ohm UI > UI Settings.</summary>
        public static OhmUISettings Instance
        {
            get
            {
                // Cache the locator rather than the config, so swapping the active config is picked up
                // immediately with no cache to invalidate.
                if (locator == null)
                    locator = Resources.Load<OhmUISettingsLocator>(OhmUISettingsLocator.ResourceName);
                return locator != null ? locator.active : null;
            }
        }
    }
}
