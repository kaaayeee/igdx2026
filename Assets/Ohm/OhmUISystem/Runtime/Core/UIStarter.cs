using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Ohm.UISystem
{
    public class UIStarter : MonoBehaviour
    {
        [Header("Start Configuration")]
        [Tooltip("Preferred: the screen prefab to open on start")]
        [FormerlySerializedAs("startScreenPrefab")]
        [SerializeField] private UIBase startScreen;

        [Tooltip("Legacy: Use UITypeReference (fallback if startScreen is null)")]
        [SerializeField] private UITypeReference startUI;

        [Header("Options")]
        [Tooltip("Delay in seconds before the UI starts opening.")]
        [SerializeField] private float delayBeforeStart = 0.1f;

        [Tooltip("If true, the system will ensure the active scene is completely loaded before starting.")]
        [SerializeField] private bool waitForSceneLoad = true;

        private void Start()
        {
            if (waitForSceneLoad)
            {
                StartCoroutine(WaitAndStartUI());
            }
            else
            {
                StartCoroutine(StartUIWithDelay());
            }
        }

        private IEnumerator WaitAndStartUI()
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().isLoaded);
            yield return StartCoroutine(StartUIWithDelay());
        }

        private IEnumerator StartUIWithDelay()
        {
            if (delayBeforeStart > 0f)
            {
                yield return new WaitForSeconds(delayBeforeStart);
            }

            if (UIManager.Instance == null)
            {
                Debug.LogWarning("OhmUI: UIStarter could not find UIManager.Instance.");
                yield break;
            }

            // Prefer the UI prefab reference
            if (startScreen != null)
            {
                UIManager.Instance.ShowUI(startScreen.GetType());
            }
            // Fallback to legacy UITypeReference
            else if (startUI != null && startUI.Type != null)
            {
                UIManager.Instance.ShowUI(startUI.Type);
            }
            else
            {
                Debug.LogWarning("OhmUI: UIStarter has no 'startScreen' or 'startUI' configured.");
            }
        }
    }
}
