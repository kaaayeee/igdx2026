#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq; // Dibutuhkan untuk .Find

[System.Serializable]
public class UIEntry
{
    [SerializeField, HideInInspector] public string name;
    public UIType type;
    public UIManager.UILayer layer = UIManager.UILayer.MAIN;
    
    [Header("Prefab & Instance")]
    public UIBase prefab;
    public UIBase instance; // Referensi disimpan di sini
    
    [Header("Configuration")]
    public bool pauseGame = false;
    public bool enableInput = true;

    [Header("Back System")]
    public bool isBackToPreviousUI = true;
    public UIType backToUI;
}

public enum UIType
{
    NONE = -1,
    MAINMENU,
    GAMEPLAY,
    PAUSEMENU
}

public class UIManager : SingletonMonoBehaviour<UIManager>
{
    public enum UILayer
    {
        MAIN,
        POPUP
    }

    [Header("Status")]
    [SerializeField] private UIType currentUI = UIType.NONE;
    [SerializeField] private UIType previousUI = UIType.NONE;
    [SerializeField] private UIType currentMainUI = UIType.NONE;
    [SerializeField] private UIType currentPopupUI = UIType.NONE;

    public UIType CurrentUI      => currentUI;
    public UIType PreviousUI     => previousUI;
    public UIType CurrentMainUI  => currentMainUI;
    public UIType CurrentPopupUI => currentPopupUI;

    [Header("Settings")]
    [SerializeField] private Transform parent;
    [SerializeField] private bool instantiateOnAwake = true;
    
    // Hapus Dictionary, kita hanya pakai List ini
    [SerializeField] public List<UIEntry> uiEntries; 

    public event Action<UIType> OnUIChanged;

    // --- Core Logic: Helper untuk mencari Entry ---
    private UIEntry GetEntry(UIType type)
    {
        // Pengganti Dictionary: Mencari langsung di List
        return uiEntries.Find(x => x.type == type);
    }

    public bool IsUIActive(UIType type)
    {
        var entry = GetEntry(type);
        // Pastikan entry ada DAN instance-nya sudah dibuat/diassign
        if (entry != null && entry.instance != null)
        {
            return entry.instance.isActive;
        }
        return false;
    }

    protected override void Awake()
    {
        base.Awake();
        InitUI();
    }

    public void InitUI()
    {
        currentMainUI  = UIType.NONE;
        currentPopupUI = UIType.NONE;

        foreach (var entry in uiEntries)
        {
            if (entry.prefab == null && entry.instance == null)
            {
                Debug.LogWarning($"UIEntry {entry.type} tidak memiliki prefab ataupun instance!");
                continue;
            }

            // Logika Instansiasi
            if (entry.instance == null)
            {
                // Jika belum ada instance (dari Editor), dan kita minta instantiate on awake
                if (instantiateOnAwake && entry.prefab != null)
                {
                    entry.instance = Instantiate(entry.prefab, parent);
                }
            }

            // Inisialisasi Instance (Hide di awal)
            if (entry.instance != null)
            {
                entry.instance.Hide();
            }
        }

        // Buka UI pertama jika diset
        if (currentUI != UIType.NONE)
        {
            ChangeUI(currentUI);
        }
    }

    public void ChangeUI(UIType toUI)
    {
        var toEntry = GetEntry(toUI);

        if (toEntry == null)
        {
            Debug.LogError($"UI {toUI} tidak ditemukan di List UI Entries!");
            return;
        }

        if (toEntry.instance == null)
        {
             Debug.LogError($"UI {toUI} ditemukan tapi Instance-nya null! (Cek instantiateOnAwake atau Editor Tools)");
             return;
        }

        // Cek jika sedang aktif
        if (currentUI == toUI && toEntry.instance.isActive)
            return;

        previousUI = currentUI;

        // Logika Layering
        if (toEntry.layer == UILayer.MAIN)
        {
            HideCurrentMain();
            HideCurrentPopup();

            currentMainUI  = toUI;
            currentPopupUI = UIType.NONE;
        }
        else if(toEntry.layer == UILayer.POPUP)
        {
            HideCurrentPopup();
            currentPopupUI = toUI;
        }

        ShowUI(toEntry);

        currentUI = toUI;
        OnUIChanged?.Invoke(currentUI);
    }

    private void ShowUI(UIEntry entry)
    {
        if (entry.instance == null) return;

        HandleUIConfiguration(entry);

        if (!entry.instance.isActive)
            entry.instance.Show();
    }

    public void HandleUIConfiguration(UIEntry entry)
    {
        // Contoh implementasi konfigurasi
        if (entry.pauseGame) Time.timeScale = 0;
        else Time.timeScale = 1;

        // GameplayManager.Instance.SetInput(entry.enableInput);
    }

    // Helper Hide menggunakan tipe
    private void HideUI(UIType type)
    {
        var entry = GetEntry(type);
        if (entry != null && entry.instance != null && entry.instance.isActive)
        {
            entry.instance.Hide();
        }
    }

    private void HideCurrentMain()
    {
        HideUI(currentMainUI);
    }

    private void HideCurrentPopup()
    {
        HideUI(currentPopupUI);
    }   

    public void OnEscape()
    {
        var currentEntry = GetEntry(currentUI);
        if (currentEntry == null) return;

        UIType targetUI;

        if (currentEntry.isBackToPreviousUI)
        {
            targetUI = previousUI;
        }
        else
        {
            targetUI = currentEntry.backToUI;
        }

        if (targetUI == UIType.NONE || targetUI == currentUI)
            return;

        ChangeUI(targetUI);
    }
    

    // ===================================================================================
    // EDITOR TOOLS
    // ===================================================================================

#if UNITY_EDITOR
    [ContextMenu("Instantiate UI (Editor)")]
    public void InstantiateUIInEditor()
    {
        if (parent == null)
        {
            Debug.LogError("Parent transform belum di-assign!");
            return;
        }

        foreach (var entry in uiEntries)
        {
            if (entry.prefab == null) continue;

            string uiName = entry.prefab.name;
            if (entry.instance != null)
            {
                Debug.Log($"UI {uiName} sudah terlink di Inspector.");
                continue;
            }

            Transform existing = parent.Find(entry.prefab.name);
            
            if (existing != null)
            {
                entry.instance = existing.GetComponent<UIBase>();
                Debug.Log($"Relink UI {uiName} dari Hierarchy ke List.");
            }
            else
            {
                UIBase newInstance = (UIBase)PrefabUtility.InstantiatePrefab(entry.prefab, parent);
                newInstance.Hide(); // Sembunyikan setelah instantiate
                entry.instance = newInstance;
                Debug.Log($"Instantiated & Linked: {uiName}");
            }
            
            entry.name = uiName;
        }

        // PENTING: Tandai object dirty agar Unity menyimpan perubahan di List ke disk/scene
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Delete All UI (Editor)")]
    public void DeleteAllUIInEditor()
    {
        if (parent == null) return;
        
        // Loop mundur untuk destroy aman
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            
            // Cek apakah anak ini adalah salah satu dari UI kita
            var entry = uiEntries.Find(x => x.prefab.name == child.name);
            if (entry != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                entry.instance = null; // Putuskan link referensi
            }
        }
        
        // Bersihkan sisa referensi yang mungkin menggantung (misal objeknya dihapus manual)
        foreach(var entry in uiEntries)
        {
            if (entry.instance == null) continue; // Sudah bersih
            
            // Cek apakah objectnya sebenarnya sudah missing/deleted
            if (entry.instance.gameObject == null) 
            {
                entry.instance = null;
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log("Semua UI dihapus dan referensi dibersihkan.");
    }

    private void OnValidate()
    {
        if (uiEntries == null) return;
        foreach (var entry in uiEntries)
        {
            entry.name = entry.prefab.name;
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIManager))]
public class UIManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        UIManager script = (UIManager)target;

        GUILayout.Space(20);
        GUILayout.Label("Editor Workflow Tools", EditorStyles.boldLabel);
        GUILayout.Label("Gunakan ini untuk setup scene tanpa play mode.", EditorStyles.miniLabel);

        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Instantiate UI"))
        {
            script.InstantiateUIInEditor();
        }

        if (GUILayout.Button("Delete UI"))
        {
            if (EditorUtility.DisplayDialog("Delete UI", "Hapus semua UI yang tergenerate?", "Ya", "Batal"))
            {
                script.DeleteAllUIInEditor();
            }
        }
        
        GUILayout.EndHorizontal();
    }
}
#endif