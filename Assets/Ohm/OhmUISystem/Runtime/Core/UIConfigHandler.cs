using UnityEngine;

namespace Ohm.UISystem
{
    [System.Serializable]
    public struct UIConfigData
    {
        public bool pauseGame;
        public bool enableInput;
    }

    public class UIConfigHandler : MonoBehaviour
    {
        private void OnEnable()
        {
            UIManager.OnUIConfigApplied += ProcessConfiguration;
        }

        private void OnDisable()
        {
            UIManager.OnUIConfigApplied -= ProcessConfiguration;
        }

        private void ProcessConfiguration(UIConfigData config)
        {
            if (config.pauseGame){
                Time.timeScale = 0f;
            }
            else {
                Time.timeScale = 1f;
            }

            if (config.enableInput) {
                // Enable input handling logic here
            }
            else {
                // Disable input handling logic here
            }
        }
    }
}
