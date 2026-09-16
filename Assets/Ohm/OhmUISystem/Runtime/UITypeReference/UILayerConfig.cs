#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ohm.UISystem
{
    [FilePath("ProjectSettings/OhmUILayerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UILayerConfig : ScriptableSingleton<UILayerConfig>
    {
        [Tooltip("Ordered list of UI layers. Index 0 = lowest priority (background), last = highest priority (overlay). Add, remove, or reorder freely.")]
        public List<string> layerNames = new List<string> { "Main", "Popup" };

        public void SaveConfig()
        {
            Save(true);
        }
    }
}
#endif
