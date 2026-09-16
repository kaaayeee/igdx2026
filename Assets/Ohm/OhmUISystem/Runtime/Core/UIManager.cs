using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Linq;

namespace Ohm.UISystem
{
    public class UIManager : SingletonMonoBehaviour<UIManager>
    {
        #region Public Properties

        /// <summary>The canvas the layer roots live under. Created by EnsureCanvas at startup.</summary>
        public Transform Parent => parent;

        public event Action<Type> OnUIChanged;
        public static event Action<UIConfigData> OnUIConfigApplied;

        /// <summary>Returns the topmost (highest-priority layer) active UI type.</summary>
        public Type CurrentUI => GetTopmostActiveUI();

        /// <summary>Returns the previous UI on the same layer as the current topmost UI.</summary>
        public Type PreviousUI
        {
            get
            {
                Type current = CurrentUI;
                if (current == null) return null;
                var entry = GetEntry(current);
                if (entry == null) return null;
                if (historyPerLayer.TryGetValue(entry.layer, out var stack) && stack.Count > 0)
                    return stack.Peek();
                return null;
            }
        }

        #endregion

        #region Runtime State

        // The canvas the layer roots are parented into — instantiated from the config's Canvas Prefab,
        // or built in code when none is assigned.
        private Transform parent;

        private List<UIEntry> runtimeEntries = new();
        private Dictionary<Type, UIEntry> uiDictionary = new();
        private Dictionary<UIBase, UIEntry> prefabDictionary = new();
        private Dictionary<UILayer, Type> currentUIPerLayer = new();
        private Dictionary<UILayer, Stack<Type>> historyPerLayer = new();
        private Dictionary<UILayer, Transform> layerRoots = new();
        private UILayer[] cachedLayers;

        // Detached UIs live outside the layer-navigation model: pooled instances (inactive) per
        // type, plus every checked-out instance across all types.
        private Dictionary<Type, Queue<UIBase>> detachedPool = new();
        private List<UIBase> activeDetached = new();

        // Live instance census per detached type, used to enforce the Pool Size cap. Tracked
        // explicitly because an instance mid-hide-animation is in neither collection above.
        private Dictionary<Type, int> detachedInstanceCount = new();

        // Auto-hide timers for UIs shown via ShowUIForDuration, keyed by the shown instance.
        private Dictionary<UIBase, Coroutine> autoHideTimers = new();

        // The EventSystem this manager created, if any — dropped as soon as a scene supplies its own.
        private EventSystem ownedEventSystem;

        // False until the first sceneLoaded callback, which fires for the scene the manager booted into.
        private bool hasLoadedScene;

        #endregion

        #region Public Query API

        public Type GetCurrentUI(UILayer layer)
        {
            return currentUIPerLayer.TryGetValue(layer, out var t) ? t : null;
        }

        public Type GetPreviousUI(UILayer layer)
        {
            if (historyPerLayer.TryGetValue(layer, out var stack) && stack.Count > 0)
                return stack.Peek();
            return null;
        }

        public int GetHistoryCount(UILayer layer)
        {
            if (historyPerLayer.TryGetValue(layer, out var stack))
                return stack.Count;
            return 0;
        }

        /// <summary>Every UI type currently registered — the project defaults plus any scene-registered UIs.</summary>
        public IEnumerable<Type> RegisteredUITypes => uiDictionary.Keys;

        /// <summary>True when this UI type is currently provided by a scene instance rather than a prefab.</summary>
        public bool IsSceneRegistered(Type type) => GetEntry(type)?.isSceneInstance ?? false;

        public bool IsUIActive<T>() where T : UIBase => IsUIActive(typeof(T));

        public bool IsUIActive(Type type)
        {
            var entry = GetEntry(type);
            if (entry != null && entry.instance != null && entry.instance.isActive)
                return true;

            // Detached UIs are tracked separately (there may be several of the same type).
            foreach (var instance in activeDetached)
            {
                if (instance != null && instance.GetType() == type && instance.isActive)
                    return true;
            }
            return false;
        }

        #endregion

        #region Global Bootstrap

        /// <summary>Creates the global UIManager before the first scene loads, so no scene needs any UI setup.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var settings = OhmUISettings.Instance;
            if (settings == null || !settings.autoBootstrap) return;
            if (Instance != null) return;

            var go = new GameObject(nameof(UIManager));
            DontDestroyOnLoad(go);
            go.AddComponent<UIManager>(); // Awake runs here, building the canvas and the layer roots
        }

        // Public rather than internal: Editor code compiles into Assembly-CSharp-Editor, a separate
        // assembly that cannot see this one's internals.
        /// <summary>Default canvas settings, used when no Canvas Prefab is assigned. Shared with the editor's scaler check.</summary>
        public static readonly Vector2 DefaultReferenceResolution = new(1920f, 1080f);
        public const CanvasScaler.ScaleMode DefaultScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        /// <summary>
        /// Gives the manager the canvas its layer roots live under: an assigned Canvas Prefab, or a
        /// built-in Overlay canvas. Must run before EnsureLayerRoots, which parents into it.
        /// </summary>
        private void EnsureCanvas()
        {
            if (parent != null) return; // a hand-placed manager may already point at one

            var canvasPrefab = OhmUISettings.Instance != null ? OhmUISettings.Instance.canvasPrefab : null;
            if (canvasPrefab != null)
            {
                // instantiateInWorldSpace: false — a World Space canvas prefab would otherwise keep
                // its authored world transform instead of sitting under the manager.
                var instance = Instantiate(canvasPrefab, transform, false);
                instance.name = canvasPrefab.name;
                parent = instance.transform;
                return;
            }

            var go = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            int uiLayer = LayerMask.NameToLayer("UI"); // -1 if the project renamed the built-in layer
            if (uiLayer >= 0) go.layer = uiLayer;

            go.transform.SetParent(transform, false);

            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = DefaultScaleMode;
            scaler.referenceResolution = DefaultReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            parent = go.transform;
        }

        /// <summary>Applies UIConfigData side-effects when no scene provides a handler. Must run before the start UI is shown.</summary>
        private void EnsureConfigHandler()
        {
            if (FindAnyObjectByType<UIConfigHandler>() == null)
                gameObject.AddComponent<UIConfigHandler>();
        }

        /// <summary>
        /// Keeps exactly one EventSystem alive: the loaded scene's when it has one, otherwise ours.
        /// Re-checked on every scene load, since scenes disagree about whether they ship one.
        /// </summary>
        private void EnsureEventSystem()
        {
            var external = FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                .FirstOrDefault(e => e != ownedEventSystem);

            if (external != null)
            {
                // The scene brought its own — drop ours rather than leaving two competing.
                if (ownedEventSystem != null) Destroy(ownedEventSystem.gameObject);
                ownedEventSystem = null;
                return;
            }

            if (ownedEventSystem != null) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            ownedEventSystem = eventSystem.GetComponent<EventSystem>();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEventSystem();

            // The bootstrap subscribes before the first scene loads, so that first callback is not a
            // scene *change* — closing there would dismiss the start UI on frame one. Additive loads
            // add to the current scene rather than replacing it, so they aren't a change either.
            bool isSceneChange = hasLoadedScene && mode == LoadSceneMode.Single;
            hasLoadedScene = true;

            if (isSceneChange && OhmUISettings.Instance != null && OhmUISettings.Instance.closeAllOnSceneChange)
                CloseAllUI(instant: true);
        }

        #endregion

        #region Initialization

        protected override void Awake()
        {
            base.Awake();

            // base.Awake destroys a duplicate but execution continues — don't let it initialize too.
            if (Instance != this)
            {
                Debug.LogError($"OhmUI: A second UIManager was found in scene '{gameObject.scene.name}' and was destroyed. " +
                               "The global UIManager is created automatically — remove this one, and register scene-placed UIs " +
                               "with a UIBakedHandler or add their prefabs to Project Settings > Ohm UI > UI Settings.");
                return;
            }

            EnsureCanvas(); // must precede InitUI — EnsureLayerRoots parents into the canvas
            EnsureConfigHandler();
            InitUI();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // The first scene has not loaded during Awake, so its EventSystem isn't discoverable until Start.
        private void Start() => EnsureEventSystem();

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void InitUI()
        {
            CancelAllAutoHideTimers();

            runtimeEntries.Clear();
            uiDictionary.Clear();
            prefabDictionary.Clear();
            currentUIPerLayer.Clear();
            historyPerLayer.Clear();
            detachedPool.Clear();
            activeDetached.Clear();
            detachedInstanceCount.Clear();

            // Cache all layer values sorted by priority (ascending)
            cachedLayers = ((UILayer[])Enum.GetValues(typeof(UILayer)))
                .OrderBy(l => (int)l)
                .ToArray();

            // Initialize per-layer tracking
            foreach (var layer in cachedLayers)
            {
                currentUIPerLayer[layer] = null;
                historyPerLayer[layer] = new Stack<Type>();
            }

            // Create the per-layer wrapper objects before any UI is spawned into them
            EnsureLayerRoots();

            var settings = OhmUISettings.Instance;
            if (settings == null)
            {
                Debug.LogWarning("OhmUI: No active UI config — assign one in Project Settings > Ohm UI > UI Settings.");
                return;
            }

            foreach (var uiPrefab in settings.defaultUIPrefabs)
                TryRegisterPrefab(uiPrefab);

            if (uiDictionary.Count == 0)
            {
                Debug.LogWarning("OhmUI: No UI prefabs registered — add them to the active config's Default UI Prefabs list.");
                return;
            }

            // Open the configured start UI (optional).
            var start = settings.startUI;
            if (start != null && !start.IsNone())
            {
                if (uiDictionary.ContainsKey(start.Type))
                    ShowUI(start.Type);
                else
                    Debug.LogWarning($"OhmUI: Start UI '{start.Type.Name}' is not registered.");
            }
        }

        private void TryRegisterPrefab(UIBase uiPrefab)
        {
            if (uiPrefab == null) return;

            Type prefabType = uiPrefab.GetType();
            if (uiDictionary.ContainsKey(prefabType))
            {
                Debug.LogWarning($"OhmUI: Duplicate UI Type found: {prefabType.Name} on UI prefab '{uiPrefab.name}' — skipped.");
                return;
            }

            RegisterEntry(new UIEntry(uiPrefab));
        }

        /// <summary>Adds an entry to the lookups and runs its spawn-time setup. The caller owns the duplicate-type check.</summary>
        private void RegisterEntry(UIEntry entry)
        {
            if (entry.prefab == null) return;

            runtimeEntries.Add(entry);
            uiDictionary[entry.PrefabType] = entry;
            prefabDictionary[entry.prefab] = entry;

            // Detached UIs opt out of the single-instance layer model — set up their pool instead.
            if (entry.detached)
            {
                InitDetachedPool(entry);
                return;
            }

            if (entry.instance == null && entry.spawnBehavior == SpawnBehavior.PrewarmOnAwake)
            {
                entry.instance = Instantiate(entry.prefab, GetLayerRoot(entry.layer));
                entry.instance.name = entry.prefab.name;
            }

            if (entry.instance == null) return;

            // Adopt instances spawned before this UI's layer existed, or into the wrong layer
            var layerRoot = GetLayerRoot(entry.layer);
            if (entry.instance.transform.parent != layerRoot)
                entry.instance.transform.SetParent(layerRoot, false);

            entry.instance.Hide(instant: true);
        }

        /// <summary>The transform the layer roots live under. Falls back to this manager if no parent is assigned.</summary>
        private Transform layerContainer => parent != null ? parent : transform;

        /// <summary>
        /// Creates (or adopts) one wrapper object per UILayer under the parent, ordered so that a
        /// higher layer is a later sibling — uGUI draws later siblings on top, so Popup renders above Main.
        /// </summary>
        public void EnsureLayerRoots()
        {
            layerRoots.Clear();

            // Callable from the editor before InitUI has run
            cachedLayers ??= ((UILayer[])Enum.GetValues(typeof(UILayer)))
                .OrderBy(l => (int)l)
                .ToArray();

            // cachedLayers is sorted by priority ascending, so pushing each root to the end in
            // turn leaves them in layer order, after any unrelated children already in the parent.
            foreach (var layer in cachedLayers)
            {
                GetLayerRoot(layer).SetAsLastSibling();
            }
        }

        /// <summary>Name of the wrapper object holding every UI on a given layer.</summary>
        public static string GetLayerRootName(UILayer layer) => $"Layer_{layer}";

        /// <summary>Returns the wrapper object for a layer, creating it if it does not exist yet.</summary>
        public Transform GetLayerRoot(UILayer layer)
        {
            if (layerRoots.TryGetValue(layer, out var cached) && cached != null)
                return cached;

            string rootName = GetLayerRootName(layer);
            Transform container = layerContainer;

            // Reuse a root that was baked into the scene rather than creating a duplicate
            Transform root = container.Find(rootName);

            if (root == null)
            {
                var go = new GameObject(rootName, typeof(RectTransform));
                root = go.transform;
                root.SetParent(container, false);

                var rect = (RectTransform)root;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition3D = Vector3.zero;
                rect.localScale = Vector3.one;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create UI Layer Root");
#endif
            }

            layerRoots[layer] = root;
            return root;
        }

        #endregion

        #region Scene-Registered UIs

        /// <summary>
        /// Registers a UI instance placed in a scene (see UIBakedHandler) so it can hold hard references to
        /// scene objects. It takes over its UI type for as long as the scene is loaded. Returns false if rejected.
        /// </summary>
        public bool RegisterInstance(UIBase instance)
        {
            if (instance == null) return false;

            Type type = instance.GetType();

            // ponytail: baked instances can't be Detached — that path pools and clones its "prefab",
            // which here is the scene instance itself. Move the UI into the UI Settings config if it needs pooling.
            if (instance.Detached)
            {
                Debug.LogWarning($"OhmUI: '{instance.name}' is Detached and cannot be registered from a scene. Untick Detached, or move it to Project Settings > Ohm UI > UI Settings.");
                return false;
            }

            var displaced = GetEntry(type);
            if (displaced != null)
            {
                if (displaced.isSceneInstance)
                {
                    Debug.LogWarning($"OhmUI: '{instance.name}' was not registered — another scene instance of {type.Name} already owns that UI type.");
                    return false;
                }

                if (displaced.detached)
                {
                    Debug.LogWarning($"OhmUI: '{instance.name}' cannot override {type.Name} — the registered UI is Detached and its pool cannot be swapped out.");
                    return false;
                }

                // The project-wide entry steps aside, hidden, and is restored on unregister.
                if (displaced.instance != null && displaced.instance.isActive)
                    displaced.instance.Hide(instant: true);
                if (GetCurrentUI(displaced.layer) == type)
                    currentUIPerLayer[displaced.layer] = null;

                runtimeEntries.Remove(displaced);
                uiDictionary.Remove(type);
                if (displaced.prefab != null)
                    prefabDictionary.Remove(displaced.prefab);
            }

            RegisterEntry(new UIEntry(instance) { instance = instance, isSceneInstance = true, displaced = displaced });
            return true;
        }

        /// <summary>Removes a scene-registered UI, destroys it, and restores the project-wide UI it displaced.</summary>
        public void UnregisterInstance(UIBase instance)
        {
            if (instance == null) return;

            Type type = instance.GetType();
            var entry = GetEntry(type);
            if (entry == null || !entry.isSceneInstance || entry.instance != instance) return;

            CancelAutoHideTimer(instance);

            if (GetCurrentUI(entry.layer) == type)
                currentUIPerLayer[entry.layer] = null;

            PurgeFromHistory(type);

            runtimeEntries.Remove(entry);
            uiDictionary.Remove(type);
            prefabDictionary.Remove(entry.prefab);

            // Registering re-parented it into DontDestroyOnLoad, so its scene no longer cleans it up.
            Destroy(instance.gameObject);

            if (entry.displaced != null)
                RegisterEntry(entry.displaced);

            OnUIChanged?.Invoke(CurrentUI);
        }

        /// <summary>Drops a type from every layer's back history — a dead type left on a stack makes a later ShowPreviousUI resolve to an unregistered UI.</summary>
        private void PurgeFromHistory(Type type)
        {
            foreach (var layer in historyPerLayer.Keys.ToList())
            {
                var stack = historyPerLayer[layer];
                if (!stack.Contains(type)) continue;

                // Enumerating a Stack yields top-first, so reverse to rebuild it in push order.
                historyPerLayer[layer] = new Stack<Type>(stack.Where(t => t != type).Reverse());
            }
        }

        #endregion

        #region Public Navigation API

        /// <summary>
        /// Navigate to and show a UI via generic type. A show that lands on the layer's history top is
        /// treated as a back step automatically. recordInHistory: null uses the prefab's Record In History
        /// setting; true/false overrides it for this call — it controls whether the UI being shown is
        /// recorded when something later replaces it, not whether this navigation is recorded.
        /// showInstant skips the incoming UI's transition; hideCurrentInstant skips it for everything this
        /// call dismisses. Returns the shown instance, or null on failure.
        /// </summary>
        public UIBase ShowUI<T>(bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false) where T : UIBase
        {
            return ShowUI(typeof(T), recordInHistory, showInstant, hideCurrentInstant);
        }

        /// <summary>Navigate to and show a UI via System.Type. Returns the shown instance, or null on failure.</summary>
        public UIBase ShowUI(Type toUI, bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false)
            => ShowUI(toUI, recordInHistory, showInstant, hideCurrentInstant, null);

        /// <summary>Navigate to a UI via System.Type, injecting a typed data payload. Returns the shown instance, or null on failure.</summary>
        public UIBase ShowUI<TData>(Type toUI, TData data, bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false)
            => ShowUI(toUI, recordInHistory, showInstant, hideCurrentInstant, instance => InjectData(instance, data));

        /// <summary>Navigate to a UI by type, injecting a typed data payload (preferred DI API). Returns the shown instance, or null on failure.</summary>
        public UIBase ShowUI<TUI, TData>(TData data, bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false) where TUI : UIBase
            => ShowUI(typeof(TUI), recordInHistory, showInstant, hideCurrentInstant, instance => InjectData(instance, data));

        /// <summary>Navigates back to the previous UI on the topmost active layer. Returns the shown instance, or null.</summary>
        public UIBase ShowPreviousUI(bool showInstant = false, bool hideCurrentInstant = false)
        {
            Type topUI = CurrentUI;
            if (topUI == null) return null;

            var entry = GetEntry(topUI);
            if (entry == null) return null;

            return ShowPreviousUI(entry.layer, showInstant, hideCurrentInstant);
        }

        /// <summary>Navigates back to the previous UI recorded on a layer. No-op when the layer has no history. Returns the shown instance, or null.</summary>
        public UIBase ShowPreviousUI(UILayer layer, bool showInstant = false, bool hideCurrentInstant = false)
        {
            // The target is the history top by construction, so the core resolves this as a back step.
            if (historyPerLayer.TryGetValue(layer, out var stack) && stack.Count > 0)
                return ShowUI(stack.Peek(), null, showInstant, hideCurrentInstant, null);
            return null;
        }

        /// <summary>Hides the active UI on every layer and clears all navigation history.</summary>
        public void CloseAllUI(bool instant = false)
        {
            if (cachedLayers == null) return;

            foreach (var layer in cachedLayers)
            {
                Type activeUI = GetCurrentUI(layer);
                if (activeUI != null)
                    HideUI(activeUI, instant);

                currentUIPerLayer[layer] = null;
                if (historyPerLayer.TryGetValue(layer, out var stack))
                    stack.Clear();
            }

            // Hide every detached UI too (each recycles itself via Hidden).
            for (int i = activeDetached.Count - 1; i >= 0; i--)
            {
                var instance = activeDetached[i];
                if (instance != null)
                    instance.Hide(instant);
            }

            OnUIChanged?.Invoke(null);
        }

        /// <summary>Closes an entire layer: hides its active navigation UI (without restoring the previous UI) and hides every detached UI on that layer, recycling pooled instances.</summary>
        public void CloseLayer(UILayer layer, bool instant = false)
        {
            CloseLayerInternal(layer, instant);
            CloseDetachedOnLayer(layer, instant);
        }

        /// <summary>Hides a specific UI on its layer without restoring the previous UI. No-op if it isn't the active UI on that layer.</summary>
        public void CloseUI(Type toUI, bool instant = false)
        {
            var entry = GetEntry(toUI);
            if (entry == null)
            {
                Debug.LogError($"OhmUI: Cannot CloseUI — UI {toUI} is not registered in the UIManager UI list!");
                return;
            }

            UILayer layer = entry.layer;

            // Only the UI actually showing on its layer can be closed.
            if (GetCurrentUI(layer) != toUI)
                return;

            CloseLayerInternal(layer, instant); // hides current, nulls the layer slot, fires OnUIChanged(CurrentUI)

            // Full dismiss: drop this layer's back history so no stale entry lingers.
            if (historyPerLayer.TryGetValue(layer, out var stack))
                stack.Clear();
        }

        /// <summary>Close a UI via generic type.</summary>
        public void CloseUI<T>(bool instant = false) where T : UIBase => CloseUI(typeof(T), instant);

        /// <summary>Close a UI via its prefab reference, or a detached instance returned by ShowUI (recycles it into the pool).</summary>
        public void CloseUI(UIBase uiPrefab, bool instant = false)
        {
            if (uiPrefab == null)
            {
                Debug.LogError("OhmUI: Cannot CloseUI — UI prefab is null!");
                return;
            }

            // A checked-out detached instance hides itself and recycles via the Hidden event.
            if (activeDetached.Contains(uiPrefab))
            {
                uiPrefab.Hide(instant);
                return;
            }

            var entry = GetEntryByPrefab(uiPrefab) ?? GetEntry(uiPrefab.GetType());
            if (entry == null)
            {
                Debug.LogError($"OhmUI: Cannot CloseUI — prefab '{uiPrefab.name}' is not registered in the UIManager UI list!");
                return;
            }

            CloseUI(entry.PrefabType, instant);
        }

        /// <summary>Back-navigate from the topmost active UI (Escape key / global back).</summary>
        public void OnEscape(bool instant = false)
        {
            Type topUI = CurrentUI;
            if (topUI == null) return;
            OnEscape(topUI, instant);
        }

        /// <summary>Back-navigate from a specific UI, only if it is the active UI on its layer.</summary>

        public void OnEscape(Type toUI, bool instant = false)
        {
            var currentEntry = GetEntry(toUI);
            if (currentEntry == null) return;

            UILayer currentLayer = currentEntry.layer;

            // Only escape a UI that is actually the active one on its layer.
            if (GetCurrentUI(currentLayer) != toUI) return;

            if (currentEntry.isBackToPreviousUI)
            {
                if (historyPerLayer.TryGetValue(currentLayer, out var stack) && stack.Count > 0)
                {
                    ShowPreviousUI(currentLayer, instant, instant);
                }
                else
                {
                    CloseLayerInternal(currentLayer, instant);
                }
            }
            else
            {
                // Use the explicit back target prefab
                Type targetUI = GetEntryByPrefab(currentEntry.backTo)?.PrefabType;

                if (historyPerLayer.ContainsKey(currentLayer))
                    historyPerLayer[currentLayer].Clear();

                if (targetUI == null || targetUI == toUI)
                    return;

                // Check if target is already visible on a different (lower) layer
                var targetEntry = GetEntry(targetUI);
                if (targetEntry != null && targetEntry.layer != currentLayer)
                {
                    Type activeOnTargetLayer = GetCurrentUI(targetEntry.layer);
                    if (activeOnTargetLayer == targetUI)
                    {
                        CloseLayerInternal(currentLayer, instant);
                        return;
                    }
                }

                ShowUI(targetUI, null, instant, instant);
            }
        }

        /// <summary>Back-navigate from a specific UI via generic type.</summary>
        public void OnEscape<T>(bool instant = false) where T : UIBase => OnEscape(typeof(T), instant);

        #endregion

        #region Timed Navigation API

        /// <summary>Shows a UI for a fixed duration (seconds, unscaled) via generic type, then hides it automatically. Always shown detached: no history, no back navigation, no auto-hide of other layers. Returns the shown instance, or null on failure.</summary>
        public UIBase ShowUIForDuration<T>(float duration) where T : UIBase
            => ShowUIForDuration(typeof(T), duration, null);

        /// <summary>Shows a UI for a fixed duration (seconds, unscaled) via System.Type, then hides it automatically. Returns the shown instance, or null on failure.</summary>
        public UIBase ShowUIForDuration(Type toUI, float duration)
            => ShowUIForDuration(toUI, duration, null);

        /// <summary>Shows a UI for a fixed duration via System.Type, injecting a typed data payload. Returns the shown instance, or null on failure.</summary>
        public UIBase ShowUIForDuration<TData>(Type toUI, TData data, float duration)
            => ShowUIForDuration(toUI, duration, instance => InjectData(instance, data));

        /// <summary>Shows a UI for a fixed duration by type, injecting a typed data payload. Returns the shown instance, or null on failure.</summary>
        public UIBase ShowUIForDuration<TUI, TData>(TData data, float duration) where TUI : UIBase
            => ShowUIForDuration(typeof(TUI), duration, instance => InjectData(instance, data));

        private UIBase ShowUIForDuration(Type toUI, float duration, Action<UIBase> configure)
        {
            var entry = GetEntry(toUI);
            if (entry == null)
            {
                Debug.LogError($"OhmUI: UI {toUI} is not registered in the UIManager UI list!");
                return null;
            }

            // A timed show checks out its own pooled instance. If this UI is also the active
            // navigation UI on its layer, that would put two copies of it on screen at once.
            if (!entry.detached && GetCurrentUI(entry.layer) == toUI)
            {
                Debug.LogWarning($"OhmUI: Cannot ShowUIForDuration — '{entry.name}' is already showing as the active UI on layer {entry.layer}. A timed show would spawn a second copy.");
                return null;
            }

            var instance = ShowUI(toUI, false, false, false, configure, forceDetached: true);
            if (instance == null) return null;

            StartAutoHideTimer(instance, duration);
            return instance;
        }

        #endregion

        #region UI Transition Logic

        private static void InjectData<TData>(UIBase instance, TData data)
        {
            if (instance is IUIInjectable<TData> injectable)
                injectable.Inject(data);
            else
                Debug.LogWarning($"OhmUI: {instance.GetType().Name} does not implement IUIInjectable<{typeof(TData).Name}> — data not injected.");
        }

        private UIBase ShowUI(Type toUI, bool? recordInHistory, bool showInstant, bool hideCurrentInstant,
                              Action<UIBase> configureInstance, bool forceDetached = false)
        {
            var toEntry = GetEntry(toUI);

            if (toEntry == null)
            {
                Debug.LogError($"OhmUI: UI {toUI} is not registered in the UIManager UI list!");
                return null;
            }

            // Detached UIs bypass the layer-navigation model entirely. Timed shows force this path
            // so they never touch history, the layer slot, or the UI config side-effects.
            if (toEntry.detached || forceDetached)
                return ShowDetached(toEntry, configureInstance, showInstant);

            // Fault-tolerant lazy instantiation
            if (toEntry.instance == null)
            {
                // A scene entry's "prefab" is the instance itself — there is nothing to respawn from.
                if (toEntry.isSceneInstance)
                {
                    Debug.LogError($"OhmUI: UI {toUI} failed to open — its scene instance was destroyed while still registered. Unregister it before destroying it.");
                    return null;
                }

                if (toEntry.prefab != null)
                {
                    toEntry.instance = Instantiate(toEntry.prefab, GetLayerRoot(toEntry.layer));
                    toEntry.instance.name = toEntry.prefab.name;
                    toEntry.instance.Hide(instant: true);
                }
                else
                {
                    Debug.LogError($"OhmUI: UI {toUI} failed to open. Instance is null and no Prefab is assigned!");
                    return null;
                }
            }

            configureInstance?.Invoke(toEntry.instance);

            UILayer targetLayer = toEntry.layer;
            Type currentOnTarget = GetCurrentUI(targetLayer);

            // --- History Management ---
            // A show that lands on the layer's history top is a back step: consume the breadcrumb
            // instead of pushing. Otherwise record the outgoing UI (unless it opted out).
            var history = historyPerLayer[targetLayer];
            if (history.Count > 0 && history.Peek() == toUI)
            {
                history.Pop();
            }
            else if (currentOnTarget != null && currentOnTarget != toUI)
            {
                // A UI shown with recordInHistory off is never pushed — the history system skips it.
                var currentEntry = GetEntry(currentOnTarget);
                if (currentEntry == null || currentEntry.recordInHistory)
                    history.Push(currentOnTarget);
            }

            // Per-call override wins; otherwise the prefab's Record In History setting applies.
            toEntry.recordInHistory = recordInHistory ?? toEntry.defaultRecordInHistory;

            // --- Hide current UI on target layer ---
            if (currentOnTarget != null && currentOnTarget != toUI)
            {
                HideUI(currentOnTarget, hideCurrentInstant);
            }

            // --- Clear all layers ABOVE target layer ---
            for (int i = cachedLayers.Length - 1; i >= 0; i--)
            {
                if ((int)cachedLayers[i] <= (int)targetLayer) break;

                UILayer upperLayer = cachedLayers[i];
                Type upperUI = GetCurrentUI(upperLayer);
                if (upperUI != null)
                {
                    HideUI(upperUI, hideCurrentInstant);
                    currentUIPerLayer[upperLayer] = null;
                    historyPerLayer[upperLayer].Clear();
                }
            }

            // --- Show the new UI ---
            ActivateInstance(toEntry, showInstant);
            currentUIPerLayer[targetLayer] = toUI;

            OnUIChanged?.Invoke(toUI);
            return toEntry.instance;
        }

        #region Detached / Pooled

        /// <summary>Creates the pool bucket for a detached UI, adopting any baked instances and prewarming up to Pool Size if configured.</summary>
        private void InitDetachedPool(UIEntry entry)
        {
            Type type = entry.PrefabType;
            if (!detachedPool.TryGetValue(type, out var pool))
            {
                pool = new Queue<UIBase>();
                detachedPool[type] = pool;
            }

            // Adopt any instances baked into the scene into the pool (hidden).
            var layerRoot = GetLayerRoot(entry.layer);
            foreach (var child in layerContainer.GetComponentsInChildren<UIBase>(true))
            {
                if (child.GetType() != type) continue;
                if (child.transform.parent != layerRoot)
                    child.transform.SetParent(layerRoot, false);
                child.Hide(instant: true);
                if (pool.Contains(child)) continue;
                pool.Enqueue(child);
                detachedInstanceCount[type] = GetDetachedInstanceCount(type) + 1;
            }

            // Prewarm up to Pool Size if requested, counting anything already baked.
            if (entry.spawnBehavior == SpawnBehavior.PrewarmOnAwake && entry.prefab != null)
            {
                int target = entry.pooled ? entry.poolSize : 1;
                while (GetDetachedInstanceCount(type) < target)
                {
                    var prewarmed = InstantiateDetached(entry, layerRoot);
                    prewarmed.Hide(instant: true);
                    pool.Enqueue(prewarmed);
                }
            }
        }

        private int GetDetachedInstanceCount(Type type)
            => detachedInstanceCount.TryGetValue(type, out var count) ? count : 0;

        /// <summary>Spawns a new detached instance under its layer root and records it in the census.</summary>
        private UIBase InstantiateDetached(UIEntry entry, Transform layerRoot)
        {
            var instance = Instantiate(entry.prefab, layerRoot);
            instance.name = entry.prefab.name;

            Type type = entry.PrefabType;
            detachedInstanceCount[type] = GetDetachedInstanceCount(type) + 1;
            return instance;
        }

        /// <summary>Shows a detached UI outside the navigation model. Pooled UIs check out a fresh instance; non-pooled reuse a single one.</summary>
        private UIBase ShowDetached(UIEntry entry, Action<UIBase> configure, bool instant = false)
        {
            if (entry.prefab == null)
            {
                Debug.LogError($"OhmUI: Detached UI {entry.PrefabType} failed to open — no prefab assigned!");
                return null;
            }

            Type type = entry.PrefabType;
            UIBase instance = null;

            // Non-pooled detached UIs reuse a single instance that is already showing.
            if (!entry.pooled)
            {
                foreach (var active in activeDetached)
                {
                    if (active != null && active.GetType() == type)
                    {
                        instance = active;
                        break;
                    }
                }
            }

            if (instance == null)
                instance = AcquireDetached(entry);

            configure?.Invoke(instance);

            if (!activeDetached.Contains(instance))
            {
                activeDetached.Add(instance);
                instance.Hidden -= OnDetachedHidden;
                instance.Hidden += OnDetachedHidden;
            }

            if (!instance.isActive)
                instance.Show(instant);

            return instance;
        }

        /// <summary>Dequeues an instance from the pool, recycles the oldest one at the Pool Size cap, or instantiates a new one.</summary>
        private UIBase AcquireDetached(UIEntry entry)
        {
            Type type = entry.PrefabType;
            var layerRoot = GetLayerRoot(entry.layer);

            if (detachedPool.TryGetValue(type, out var pool))
            {
                while (pool.Count > 0)
                {
                    var pooled = pool.Dequeue();
                    if (pooled != null)
                    {
                        if (pooled.transform.parent != layerRoot)
                            pooled.transform.SetParent(layerRoot, false);
                        return pooled;
                    }
                }
            }

            // Hard cap: with Dynamic Pooling off, steal the oldest showing instance instead of growing.
            if (entry.pooled && !entry.dynamicPooling && GetDetachedInstanceCount(type) >= entry.poolSize)
            {
                var recycled = RecycleOldestActive(type);
                if (recycled != null)
                {
                    if (recycled.transform.parent != layerRoot)
                        recycled.transform.SetParent(layerRoot, false);
                    return recycled;
                }
            }

            return InstantiateDetached(entry, layerRoot);
        }

        /// <summary>Hard-cap fallback: takes the longest-showing instance of a type for immediate reuse.</summary>
        private UIBase RecycleOldestActive(Type type)
        {
            // activeDetached is append-ordered, so the first match is the oldest checked-out instance.
            for (int i = 0; i < activeDetached.Count; i++)
            {
                var active = activeDetached[i];
                if (active == null || active.GetType() != type) continue;

                // Unhook and de-register BEFORE hiding: this instance goes straight back to
                // ShowDetached (which re-registers it), so it must not travel the normal
                // hide -> pool recycle path. Other Hidden subscribers still fire — notably the
                // auto-hide timer, whose stale countdown must be cancelled.
                active.Hidden -= OnDetachedHidden;
                activeDetached.RemoveAt(i);
                active.Hide(instant: true);
                return active;
            }
            return null;
        }

        /// <summary>
        /// Recycles a hidden detached instance. Hide() fires Hidden as soon as the hide is requested,
        /// but a TransitionController plays its hide animation asynchronously and only disables the
        /// object afterward — so the return to the pool is deferred by the hide-animation length to
        /// avoid pooling (and re-checking-out) an instance that is still visible on screen.
        /// </summary>
        private void OnDetachedHidden(UIBase instance)
        {
            if (instance == null) return;
            if (!activeDetached.Remove(instance)) return;

            float hideDuration = instance.HideAnimationDuration;
            if (hideDuration > 0f)
                StartCoroutine(ReturnToPoolAfter(instance, hideDuration));
            else
                ReturnToPool(instance);
        }

        /// <summary>Waits out the hide animation, then pools the instance unless it was checked out again.</summary>
        private IEnumerator ReturnToPoolAfter(UIBase instance, float delay)
        {
            // Unscaled to match the timed API / detached UIs that run while the game is paused.
            yield return new WaitForSecondsRealtime(delay);
            if (instance == null) yield break;

            // If it was re-shown during the hide animation, leave it alone.
            if (instance.isActive || activeDetached.Contains(instance)) yield break;

            ReturnToPool(instance);
        }

        /// <summary>Enqueues a fully-hidden detached instance into its type's pool.</summary>
        private void ReturnToPool(UIBase instance)
        {
            Type type = instance.GetType();
            if (!detachedPool.TryGetValue(type, out var pool))
            {
                pool = new Queue<UIBase>();
                detachedPool[type] = pool;
            }
            if (!pool.Contains(instance))
                pool.Enqueue(instance);
        }

        /// <summary>Hides every active detached instance on a layer (each recycles itself via Hidden).</summary>
        private void CloseDetachedOnLayer(UILayer layer, bool instant = false)
        {
            for (int i = activeDetached.Count - 1; i >= 0; i--)
            {
                var instance = activeDetached[i];
                if (instance != null && instance.Layer == layer)
                    instance.Hide(instant);
            }
        }

        #endregion

        #region Auto-Hide Timers

        /// <summary>Starts (or restarts) the auto-hide countdown for a timed instance.</summary>
        private void StartAutoHideTimer(UIBase instance, float duration)
        {
            CancelAutoHideTimer(instance);

            // Hidden fires whenever the UI is dismissed early (Hide/CloseUI/CloseLayer/CloseAllUI).
            // Cancelling there stops a stale timer from hiding a pooled instance that has since
            // been recycled and checked out again.
            instance.Hidden += OnAutoHideTargetHidden;
            autoHideTimers[instance] = StartCoroutine(AutoHideRoutine(instance, duration));
        }

        private IEnumerator AutoHideRoutine(UIBase instance, float duration)
        {
            // Unscaled so a timed UI still dismisses itself while a UIConfigData has paused the game.
            yield return new WaitForSecondsRealtime(duration);

            // Clear bookkeeping BEFORE Hide(): Hide fires Hidden, whose handler would otherwise
            // StopCoroutine this very routine mid-execution.
            autoHideTimers.Remove(instance);
            if (instance == null) yield break;

            instance.Hidden -= OnAutoHideTargetHidden;

            if (instance.isActive)
                instance.Hide();
        }

        private void OnAutoHideTargetHidden(UIBase instance) => CancelAutoHideTimer(instance);

        private void CancelAutoHideTimer(UIBase instance)
        {
            if (instance == null) return;

            if (autoHideTimers.TryGetValue(instance, out var routine))
            {
                if (routine != null) StopCoroutine(routine);
                autoHideTimers.Remove(instance);
            }

            instance.Hidden -= OnAutoHideTargetHidden;
        }

        private void CancelAllAutoHideTimers()
        {
            foreach (var pair in autoHideTimers)
            {
                if (pair.Value != null) StopCoroutine(pair.Value);
                if (pair.Key != null) pair.Key.Hidden -= OnAutoHideTargetHidden;
            }
            autoHideTimers.Clear();
        }

        #endregion

        #endregion

        #region Show / Hide

        private void ActivateInstance(UIEntry entry, bool instant = false)
        {
            if (entry.instance == null) return;

            OnUIConfigApplied?.Invoke(entry.config);

            if (!entry.instance.isActive)
                entry.instance.Show(instant);
        }

        private void HideUI(Type type, bool instant = false)
        {
            var entry = GetEntry(type);
            if (entry != null && entry.instance != null && entry.instance.isActive)
            {
                entry.instance.Hide(instant);
            }
        }

        #endregion

        #region Internal Helpers

        /// <summary>Managed-only layer close: hides the active navigation UI and clears the layer slot. Used by nav (OnEscape/CloseUI).</summary>
        private void CloseLayerInternal(UILayer layer, bool instant = false)
        {
            Type activeUI = GetCurrentUI(layer);
            if (activeUI != null)
            {
                HideUI(activeUI, instant);
                currentUIPerLayer[layer] = null;
            }
            OnUIChanged?.Invoke(CurrentUI);
        }

        /// <summary>Returns the runtime entry for a given UI prefab.</summary>
        private UIEntry GetEntryByPrefab(UIBase uiPrefab)
        {
            if (uiPrefab == null) return null;
            return prefabDictionary.TryGetValue(uiPrefab, out var entry) ? entry : null;
        }

        private UIEntry GetEntry(Type type)
        {
            if (type == null) return null;
            return uiDictionary.TryGetValue(type, out var entry) ? entry : null;
        }

        private Type GetTopmostActiveUI()
        {
            if (cachedLayers == null) return null;
            for (int i = cachedLayers.Length - 1; i >= 0; i--)
            {
                if (currentUIPerLayer.TryGetValue(cachedLayers[i], out var t) && t != null)
                    return t;
            }
            return null;
        }

        #endregion

        #region UIEntry (runtime wrapper)

        /// <summary>
        /// Runtime-only wrapper for a registered UI prefab. UI settings are read from the prefab's
        /// UIBase fields. The 'instance' field is runtime state managed by UIManager.
        /// </summary>
        private sealed class UIEntry
        {
            public UIBase prefab;
            public UIBase instance;

            /// <summary>Runtime: whether the last Show asked the history system to record this UI (resolved from the per-call override or the prefab default).</summary>
            public bool recordInHistory;

            /// <summary>True when this entry wraps a UI placed in a scene (registered via UIBakedHandler) rather than a prefab.</summary>
            public bool isSceneInstance;

            /// <summary>The project-wide entry this one temporarily replaced; restored when the scene unregisters.</summary>
            public UIEntry displaced;

            public string name => prefab != null ? prefab.name : "(null)";
            public UILayer layer => prefab != null ? prefab.Layer : UILayer.Main;
            public SpawnBehavior spawnBehavior => prefab != null ? prefab.SpawnBehavior : SpawnBehavior.LazyLoad;
            public UIConfigData config => prefab != null ? prefab.ConfigData : default;
            public bool isBackToPreviousUI => prefab == null || prefab.IsBackToPreviousUI;
            public UIBase backTo => prefab != null ? prefab.BackTo : null;
            public bool defaultRecordInHistory => prefab == null || prefab.RecordInHistory;
            public bool detached => prefab != null && prefab.Detached;
            public bool pooled => prefab != null && prefab.Pooled;
            public int poolSize => prefab != null ? prefab.PoolSize : 1;
            public bool dynamicPooling => prefab == null || prefab.DynamicPooling;

            /// <summary>Returns the System.Type of the prefab's UIBase subclass.</summary>
            public Type PrefabType => prefab != null ? prefab.GetType() : null;

            public UIEntry(UIBase prefab)
            {
                this.prefab = prefab;
            }
        }

        #endregion
    }
}
