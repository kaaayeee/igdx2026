using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SceneReference
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset _sceneAsset;
#endif

    [SerializeField, HideInInspector] private string _scenePath;
    [SerializeField, HideInInspector] private string _sceneName;

    public string Path => _scenePath;
    public string Name => _sceneName;
    public bool IsValid => !string.IsNullOrEmpty(_scenePath);

    /// <summary>Sinkronisasi SceneAsset -> string. Dipanggil dari OnValidate.</summary>
    public void Sync()
    {
#if UNITY_EDITOR
        if (_sceneAsset != null)
        {
            _scenePath = AssetDatabase.GetAssetPath(_sceneAsset);
            _sceneName = _sceneAsset.name;
        }
        else
        {
            _scenePath = string.Empty;
            _sceneName = string.Empty;
        }
#endif
    }
}