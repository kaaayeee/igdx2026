using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : SingletonMonoBehaviour<TransitionManager> {
    [SerializeField] private UIBase currentTransition;

    protected override void Awake() {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeScene(SceneField scene) {
        StartCoroutine(TransitionRoutine(scene));
    }
    public void ChangeScene(SceneField scene, Vector2 screenPosition = default) {
        // Cek jika transisi saat ini adalah tipe Circle, maka set posisinya
        if (currentTransition is UICircleTransition circleTransition && screenPosition != default) {
            circleTransition.SetTransitionPosition(screenPosition);
        }

        ChangeScene(scene);
    }

    private IEnumerator TransitionRoutine(string sceneName) {
        if (currentTransition != null) {
            // 1. Jalankan Animasi Keluar
            currentTransition.Show();
            yield return new WaitForSecondsRealtime(currentTransition.AnimationDuration);
        }


        Debug.Log("Changing scene to: " + sceneName);
        // 2. Pindah Scene
        SceneManager.LoadScene(sceneName);

        // Tunggu satu frame untuk memastikan scene baru terinisialisasi
        yield return null;

        if (currentTransition != null) {
            // 3. Jalankan Animasi Masuk
            currentTransition.Hide();
            yield return new WaitForSecondsRealtime(currentTransition.AnimationDuration);
        }
    }

    // Method untuk mengganti jenis transisi secara runtime jika perlu
    public void SetTransitionStyle(UIBase newTransition) {
        currentTransition = newTransition;
    }
#if UNITY_EDITOR
    [SerializeField] private SceneField initialScene;

    [ContextMenu("Set Transition Style to Current")]
    private void SetTransitionStyleToCurrent() {
        ChangeScene(initialScene);
    }
#endif
}
