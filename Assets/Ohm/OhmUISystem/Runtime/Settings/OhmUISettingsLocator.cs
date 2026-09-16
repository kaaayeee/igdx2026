using UnityEngine;

namespace Ohm.UISystem
{
    /// <summary>
    /// Points at the UI config the game runs with. This is the one asset with a fixed Resources path,
    /// which is what lets the config assets themselves live anywhere and be swapped freely.
    /// </summary>
    public class OhmUISettingsLocator : ScriptableObject
    {
        public const string ResourceName = "OhmUISettingsLocator";
        public const string AssetPath = "Assets/Ohm/OhmUISystem/Resources/" + ResourceName + ".asset";

        [Tooltip("The UI Settings asset the game runs with. Switch it in Project Settings > Ohm UI > UI Settings.")]
        public OhmUISettings active;
    }
}
