using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

using Ohm.UISystem;
public class OhmUISystemDemoManager : SingletonMonoBehaviour<OhmUISystemDemoManager>
{
    [Header("Platform Rotation")]
    [Tooltip("The object rotated by the Rotate Left/Right buttons (e.g. the Plane or a Cube).")]
    [SerializeField] private Transform platform;
    [SerializeField] private float rotateStep = 90f;
    [SerializeField] private float rotateDuration = 0.4f;
    [SerializeField] private Ease rotateEase = Ease.OutCubic;

    [Header("Scene Loading")]
    // [SerializeField] private List<SceneEntry> sceneEntries;

    private Tween rotateTween;
    private float targetYaw;

    protected override void Awake()
    {
        base.Awake();
        if (platform != null)
            targetYaw = platform.localEulerAngles.y;
    }

    // --- Platform rotation (called from UIGameplay buttons) ---

    public void RotateRight() => RotatePlatform(rotateStep);
    public void RotateLeft() => RotatePlatform(-rotateStep);

    private void RotatePlatform(float delta)
    {
        if (platform == null)
        {
            Debug.LogWarning("OhmUI: OhmUISystemDemoManager has no platform assigned.");
            return;
        }

        targetYaw += delta;
        rotateTween?.Kill();
        rotateTween = platform.DOLocalRotate(new Vector3(0f, targetYaw, 0f), rotateDuration)
            .SetEase(rotateEase);
        
        Debug.Log($"OhmUI: Rotating platform to {targetYaw} degrees.");
    }

    // --- Scene loading (mirrors GameManager) ---

    // public void LoadScene(SceneType type)
    // {
    //     var entry = sceneEntries.Find(e => e.type == type);
    //     if (entry != null)
    //     {
    //         UnityEngine.SceneManagement.SceneManager.LoadScene(entry.scene);
    //         entry.isActive = true;
    //     }
    //     else
    //     {
    //         Debug.LogError($"OhmUI: Scene of type {type} not found in OhmUISystemDemoManager.");
    //     }
    // }

    // public void LoadGameplayScene() => LoadScene(SceneType.Gameplay);
    // public void LoadMainMenuScene() => LoadScene(SceneType.MainMenu);
}
