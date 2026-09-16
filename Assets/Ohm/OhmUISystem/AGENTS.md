# AGENTS.md — Ohm UI System

Instructions for AI coding assistants working in a project that uses **Ohm UI System**
(namespace `Ohm.UISystem`). Everything here is verified against the shipped runtime source.
Human-facing guide: [Documentation/OhmUISystem_README.md](Documentation/OhmUISystem_README.md).

---

## 1. Hard rules

1. **Never place a `UIManager` in a scene.** Exactly one exists; it is created in code before the
   first scene loads. A scene-placed one is destroyed at runtime and logs an error.
2. **Never `Instantiate` a UI prefab yourself.** Always `UIManager.Instance.ShowUI(...)` — direct
   instantiation skips layer roots, history, pooling, and config side-effects.
3. **Never edit `UIType.g.cs` or `UILayer.g.cs`.** They are regenerated on every script reload.
4. **A screen is only reachable if it is registered** — in the active config's **Default UI
   Prefabs**, or listed on a scene `UIBakedHandler`. Unregistered ⇒ `ShowUI` logs an error and
   returns `null`.
5. **There is no prefab-based `ShowUI`.** Pass a `Type`: `ShowUI<T>()`, `ShowUI(UIType.X)`, or
   `ShowUI(prefab.GetType())`. `CloseUI(UIBase)` is the only surviving instance-typed verb.
6. **`pooled` requires `detached`.** On its own it is ignored (and cleared by `OnValidate`).
7. **A timed show (`ShowUIForDuration`) is always detached**, regardless of the prefab's flag.
8. **Overriding `Show`/`Hide`/`OnDestroy` requires calling `base`.** Skipping `base.Hide()` leaks
   detached instances (they never return to the pool) and strands auto-hide timers.
9. **Never write `UIBase.isActive`.** It is public for the inspector but owned by `Show`/`Hide`.
10. **Hide a detached UI with `Hide()` / `CloseUI(instance)`**, never `gameObject.SetActive(false)`
    — only the former recycles it into the pool.
11. **Screen scripts live in the global namespace** (no `namespace` block) and `using Ohm.UISystem;`.
    That is what the built-in script templates emit and what `UIType` codegen expects.
12. **Do not emit the pre-v6 API.** `UINavGraph`, `UIScreenNav`, `UINavLink`, `UINavButtonList`,
    `UINavigationBinder`, `UICondition` / `[UINavCondition]`, `ChangeUI`, and the `isBackwards`
    parameter were **deleted**. See §15.

---

## 2. Architecture

```
[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] UIManager.Bootstrap()
  └─ reads OhmUISettings.Instance  (Resources/OhmUISettingsLocator.asset -> .active)
     └─ if autoBootstrap: new GameObject("UIManager") + DontDestroyOnLoad
        ├─ EnsureCanvas()        config.canvasPrefab, else a code-built Overlay canvas @1920x1080
        ├─ EnsureConfigHandler() adds UIConfigHandler if the scene has none
        └─ InitUI()
           ├─ EnsureLayerRoots()  one "Layer_<Name>" child per UILayer, sibling-ordered by priority
           ├─ registers every config.defaultUIPrefabs entry
           └─ shows config.startUI (optional)
```

- **Draw order** is sibling order: layer index 0 = lowest (background), last = highest (overlay).
  Every instance lives under `Layer_<Name>` — never directly under the canvas.
- **State** is per layer: one active UI (`GetCurrentUI(layer)`) plus one history stack
  (`GetHistoryCount(layer)`).
- **Detached UIs** live outside that model entirely: a pool per type plus a list of checked-out
  instances. No layer slot, no history.
- `UIStarter` (optional, per scene) opens a screen on scene load; `UIBakedHandler` (optional, per
  scene) registers scene-placed UI instances.
- Scene changes: the manager survives them. `closeAllOnSceneChange` on the config decides whether
  UIs are closed on a single-mode load (additive loads never count as a change).

---

## 3. Creating a screen

Menu: **Assets ▸ Create ▸ OhmUI ▸ UI Script** (or *UI Script (Injectable)*). Written by hand, the
canonical shapes are:

```csharp
using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;

public class UIShop : UIBase
{
    [Header("References")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        buyButton.onClick.AddListener(() => UIManager.Instance.ShowUI<UICheckout>());
        backButton.onClick.AddListener(() => UIManager.Instance.OnEscape());
    }
}
```

```csharp
[System.Serializable]
public struct UIShopData { public string title; }

public class UIShop : UIBase, IUIInjectable<UIShopData>
{
    public void Inject(UIShopData data) { /* apply payload */ }
}
```

Then:

1. Attach the script to the panel root, make it a **prefab**.
2. `UIBase` is `[RequireComponent(typeof(CanvasGroup))]` — the CanvasGroup is added for you.
3. Set **Layer** and **Spawn Behavior** on the `UIBase` component.
4. Register the prefab: *Project Settings ▸ Ohm UI ▸ UI Settings ▸ (Select Config) ▸ Default UI
   Prefabs*.
5. An empty subclass is valid. A screen driven entirely from elsewhere needs no code.

**Lifecycle note:** `Awake`/`Start` run when the instance is created. For `LazyLoad` that is the
first `ShowUI`, not scene start. Wire per-show logic in an overridden `Show`, or in
`OnEnable`/`OnDisable`.

---

## 4. `UIManager` API

All verbs go through `UIManager.Instance` (`Ohm.UISystem`). **Every show verb returns the shown
`UIBase`, or `null` on failure.** Close/back verbs return `void`.

```csharp
// --- Show ---
UIBase ShowUI<T>          (bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false) where T : UIBase
UIBase ShowUI             (Type toUI, bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false)
UIBase ShowUI<TData>      (Type toUI, TData data, bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false)
UIBase ShowUI<TUI, TData> (TData data, bool? recordInHistory = null, bool showInstant = false, bool hideCurrentInstant = false) where TUI : UIBase
UIBase ShowPreviousUI     (bool showInstant = false, bool hideCurrentInstant = false)
UIBase ShowPreviousUI     (UILayer layer, bool showInstant = false, bool hideCurrentInstant = false)

// --- Timed: always detached, so no recordInHistory / instant flags exist ---
UIBase ShowUIForDuration<T>          (float duration) where T : UIBase
UIBase ShowUIForDuration             (Type toUI, float duration)
UIBase ShowUIForDuration<TData>      (Type toUI, TData data, float duration)
UIBase ShowUIForDuration<TUI, TData> (TData data, float duration) where TUI : UIBase

// --- Back / close ---
void OnEscape    (bool instant = false)                // topmost active UI
void OnEscape    (Type toUI, bool instant = false)     // no-op unless it is active on its layer
void OnEscape<T> (bool instant = false) where T : UIBase
void CloseUI     (Type toUI, bool instant = false)     // no history restore; clears that layer's history
void CloseUI<T>  (bool instant = false) where T : UIBase
void CloseUI     (UIBase prefabOrDetachedInstance, bool instant = false)
void CloseLayer  (UILayer layer, bool instant = false) // managed UI + every detached UI on it
void CloseAllUI  (bool instant = false)                // everything, all history cleared

// --- Queries ---
Type              CurrentUI  { get; }     // topmost active layer's UI type, or null
Type              PreviousUI { get; }     // history top on the current UI's layer
Type              GetCurrentUI(UILayer layer)
Type              GetPreviousUI(UILayer layer)
int               GetHistoryCount(UILayer layer)
bool              IsUIActive<T>() where T : UIBase
bool              IsUIActive(Type type)   // true if any detached instance of the type is showing
IEnumerable<Type> RegisteredUITypes { get; }
bool              IsSceneRegistered(Type type)
Transform         Parent { get; }         // the canvas the layer roots live under
Transform         GetLayerRoot(UILayer layer)

// --- Events ---
event Action<Type> OnUIChanged;                       // instance event; fires with null when all closed
static event Action<UIConfigData> OnUIConfigApplied;  // static; UIConfigHandler subscribes

// --- Registration (UIBakedHandler calls these for you) ---
bool RegisterInstance(UIBase instance);   // false if rejected — see §10
void UnregisterInstance(UIBase instance); // destroys the instance and restores what it displaced
void InitUI();                            // full re-init; cancels every auto-hide timer
```

Usage:

```csharp
UIManager.Instance.ShowUI<UIInventory>();
UIManager.Instance.ShowUI(UIType.UIInventory);
UIManager.Instance.ShowUI(inventoryPrefab.GetType());
UIManager.Instance.ShowUI<UIInformation>(recordInHistory: false);
UIManager.Instance.ShowUI<UIMainMenu>(showInstant: true, hideCurrentInstant: true);

UIBase toast = UIManager.Instance.ShowUIForDuration<UIToast>(2f);
UIManager.Instance.CloseUI(toast);   // early dismiss cancels the timer
```

`showInstant` skips the **incoming** UI's transition. `hideCurrentInstant` skips it for
**everything the call dismisses** — the outgoing UI on the target layer plus every layer above it.
Close/back verbs take one `instant`, because only one thing is going away.

---

## 5. `UIBase` fields

Set on the **prefab**, read by the manager at runtime.

| Inspector field | Property | Meaning |
|---|---|---|
| Layer | `Layer` | Which `UILayer` this screen occupies. |
| Spawn Behavior | `SpawnBehavior` | `PrewarmOnAwake` / `LazyLoad` — see below. Ignored on instances registered by a `UIBakedHandler`. |
| Config Data | `ConfigData` | `pauseGame`, `enableInput`; broadcast via `OnUIConfigApplied` on show. Skipped for detached UIs. |
| Record In History | `RecordInHistory` | Record **this** UI on its layer's history when something later replaces it. See §7. |
| Is Back To Previous UI | `IsBackToPreviousUI` | On: `OnEscape` pops history. Off: always go to **Back To**. |
| Back To | `BackTo` | Fixed back target (shown only when the flag above is off). Must be registered. |
| Detached | `Detached` | Out of the navigation model: no history, no back, no auto-hide, no config. |
| Pooled | `Pooled` | Many instances at once via a pool. **Requires Detached.** |
| Pool Size | `PoolSize` | Prewarm count; with Dynamic Pooling off, also a hard cap. Clamped to ≥ 1. |
| Dynamic Pooling | `DynamicPooling` | On: grow past Pool Size on demand. Off: recycle the oldest showing instance instead. |
| Transition Controller | `TransitionController` | Optional Show/Hide animation driver. |
| First Selected | — | Button auto-selected for gamepad/keyboard navigation. |

Members you may override or subscribe to:

```csharp
public virtual void Show(bool instant = false);   // sets isActive, SetActive(true), drives the controller
public virtual void Hide(bool instant = false);   // drives the controller (or SetActive(false)), fires Hidden
protected virtual void OnDestroy();               // empty body; overrides must call base
public event Action<UIBase> Hidden;               // fired when a hide is REQUESTED, not when it finishes
public float ShowAnimationDuration / HideAnimationDuration / AnimationDuration;
public bool isActive;                             // read-only in practice — never assign it
```

Without a `TransitionController`, `Hide()` deactivates the GameObject immediately.

**Spawn Behavior**

| Value | Behavior |
|---|---|
| `PrewarmOnAwake` | Instantiated hidden during `InitUI`. For detached + pooled UIs, prewarms up to Pool Size. |
| `LazyLoad` | Instantiated on first show. |

---

## 6. Choosing a mode

| Need | Configuration | How to show / hide |
|---|---|---|
| Menu, screen, dialog in the nav flow | default (nothing ticked) | `ShowUI<T>()` / `OnEscape()` |
| Popup back navigation should skip | untick **Record In History** (or pass `recordInHistory: false`) | `ShowUI<T>()` |
| Persistent HUD / single overlay outside nav | **Detached** | `ShowUI<T>()`, then `instance.Hide()` |
| Many at once (damage numbers, toasts) | **Detached + Pooled** (+ Pool Size) | `ShowUI<T>()` per spawn; each hides itself |
| Auto-dismiss after N seconds | **Detached** (+ Pooled for several) | `ShowUIForDuration<T>(seconds)` |
| Screen needs a hard reference to a scene object | scene instance on a `UIBakedHandler` | `ShowUI<T>()` as normal |

Detached specifics:

- Not in the layer slot or history; not auto-hidden when a lower layer opens; no `OnUIChanged` and
  no `OnUIConfigApplied`. **The caller owns the hide** — only `CloseLayer` / `CloseAllUI` sweep them.
- `Detached` **without** `Pooled` = one reused instance: a second `ShowUI` returns the same object.
- `Detached + Pooled` = one instance per call, recycled on hide.
- With **Dynamic Pooling off**, a show past Pool Size steals the longest-showing instance instead of
  growing the pool.
- Pool return is **deferred by `HideAnimationDuration`**, so an instance mid-hide-animation is not
  handed straight back out.

Timed specifics:

- The countdown is **unscaled**, so it still fires while `pauseGame` holds `Time.timeScale = 0`.
- Re-showing the same instance **restarts** the countdown rather than stacking timers.
- Any early dismiss (`Hide` / `CloseUI` / `CloseLayer` / `CloseAllUI`) cancels the timer.
- `duration <= 0` warns and shows with no timer.
- Refused with a warning when the target is a non-detached UI that is currently the active UI on its
  layer — a timed show checks out its own instance, which would put two copies on screen.

---

## 7. History and back navigation

Four rules, in the order they bite:

1. **Back steps are auto-detected.** A show whose target is already the layer's history top **pops**
   instead of pushing. There is no `isBackwards` parameter, so a Back button wired as a plain
   `ShowUI` unwinds rather than growing the stack.
2. **`recordInHistory` is written on enter, read on leave.** It is stored when the UI is *shown* and
   consulted later, when that UI is the *outgoing* one. Ticking it off on screen X means "when
   something replaces X, don't remember X" — it does not stop whatever preceded X from being recorded.
3. **Same-layer only.** Each layer owns its stack; a cross-layer show never reads the flag.
4. **Empty history ⇒ `OnEscape` closes the layer** instead of popping. This is why an unrecorded
   popup chain "falls through" to the layer below.

Worked example — `Gameplay` (Main) → `PauseMenu` (Popup) → `Settings` (Popup):
untick **Record In History** on **`Settings`**, not on `PauseMenu`. Escaping Settings returns to
PauseMenu; escaping again closes the Popup layer and reveals Gameplay.

`OnEscape` follows the prefab's back rule (history, or **Back To**). `ShowPreviousUI` always walks
the history stack directly. `CloseUI` just dismisses — no restore, and it clears that layer's history.

`BackTo` is **not validated at init**. If the target prefab is not registered, `OnEscape` silently
does nothing.

---

## 8. Passing data (dependency injection)

```csharp
[System.Serializable]
public struct ConfirmData { public string title; public UnityEvent onConfirm; }

public class UIConfirmation : UIBase, IUIInjectable<ConfirmData>
{
    public void Inject(ConfirmData data) { /* apply */ }
}

// Compile-time-checked pairing (preferred):
UIManager.Instance.ShowUI<UIConfirmation, ConfirmData>(data);

// Dynamic target — the data generic is explicit because the UI is a Type:
UIManager.Instance.ShowUI<ConfirmData>(UIType.UIConfirmation, data);

// Timed variants mirror both shapes:
UIManager.Instance.ShowUIForDuration<UIToast, ToastData>(data, 2f);
UIManager.Instance.ShowUIForDuration<ToastData>(UIType.UIToast, data, 2f);
```

`Inject` runs **before** `Show`. If the screen does not implement `IUIInjectable<TData>`, the manager
logs `does not implement IUIInjectable<...> — data not injected` and shows it anyway. Reference
implementation: `Examples/Scripts/Menus/UIConfirmation.cs`; pooled example:
`Examples/Scripts/Menus/UIFloatingNumber.cs` with `Examples/Scripts/Clients/FloatingNumberClient.cs`.

---

## 9. Transitions

Setup: add `TransitionBase` components to the objects to animate → add a `TransitionController` to
the screen → **Fetch Transitions (Include Children)** → assign the controller to the `UIBase`'s
**Transition Controller** field. Per-entry **Show Delay** / **Hide Delay** stagger elements;
**Auto-Reverse Hide Delays** (component context menu) mirrors the show order for hiding.

Controller options: **Show On Enable** (snaps to the shown state on enable — it calls
`Show(instant: true)`, so no animation plays) and **Disable After Hide** (deactivates the GameObject
once the hide finishes).

Included implementations (`Examples/Scripts/Transitions/`): `FadeTransition`, `ScaleTransition`,
`SlideTransition`, `MoveTransition`, `AnimatorTransition`. Each has independent Show and Hide configs
and a **Run While Paused** toggle (unscaled time, so menus animate at `timeScale = 0`).

Custom transition — four required members:

```csharp
using Ohm.UISystem;

public class RotateTransition : TransitionBase
{
    public float duration = 0.3f;

    public override float GetDuration(bool isShow) => duration;
    public override void PrepareShow()             { /* set the pre-show state */ }
    protected override void PlayShow(bool instant) { /* animate in  */ }
    protected override void PlayHide(bool instant) { /* animate out */ }

    // Optional: enables the inspector's "Capture Current State" buttons.
    public override bool SupportsCapture => true;
    public override void CaptureShowConfig() { }
    public override void CaptureHideConfig() { }
}
```

`PrepareShow()` is skipped on an instant show. The controller calls `TriggerShow`/`TriggerHide` —
never call `PlayShow`/`PlayHide` directly.

Timing note: `UIBase.Hidden` fires when the hide is **requested**, not when the animation ends. Read
`HideAnimationDuration` if you need the real end time.

---

## 10. Scene-specific UIs (`UIBakedHandler`)

A prefab cannot reference a scene object. When a screen needs one, the screen must live in the scene:

1. Put a `UIBakedHandler` on a GameObject under the scene's Canvas.
2. Use **Bake Into Scene** to drop a UI prefab in as a linked instance, or drag an existing scene UI
   into **Baked UIs**.
3. Wire its scene references normally.

At runtime the handler registers those instances in `Awake` and unregisters (and destroys) them in
`OnDestroy`, so they behave like any other screen — same layers, same history, same `ShowUI`.

Rules and rejections:

- List **scene instances**, never prefab assets.
- One UI type = one entry. A baked instance temporarily **displaces** a project-wide entry of the
  same type and restores it when the scene unloads.
- **Rejected:** a `Detached` UI (its pool would clone scene references); a second scene instance of a
  type already scene-registered; displacing a registered entry that is itself `Detached`.
- Keep the scene canvas's `CanvasScaler` matching the global one — baked UIs are re-parented under
  the global canvas at runtime, so a mismatch shifts the layout between edit mode and play mode.
- Never `Destroy` a registered scene instance directly. Unregister first, or `ShowUI` errors with
  *its scene instance was destroyed while still registered*.

---

## 11. Layers

*Project Settings ▸ Ohm UI ▸ UI Settings ▸ **Layers*** — a reorderable list of names (default
`Main`, `Popup`). Index 0 = lowest priority, last = highest. Click **Save & Regenerate** to rebuild
the `UILayer` enum.

Layers are **project-wide** and shared by every config, because the enum is generated at compile
time. Showing a UI hides everything on **higher** layers and replaces the current UI on its own.

---

## 12. Generated files

`Runtime/UITypeReference/UIType.g.cs` and `UILayer.g.cs` regenerate on every script reload. Never
hand-edit them; expect them to show as modified in `git status`.

**Deletion hazard:** `UIType.g.cs` hard-references every screen via `typeof(X)`, and the regenerator
does not run when compilation fails. Deleting a screen script from the Unity Project window is
handled automatically. Two paths are not:

- deleting a screen script **outside Unity** (git, Explorer),
- **renaming a screen class inside its file**.

Both leave a `typeof(DeletedType)` that breaks the compile. Recovery: **Tools ▸ Ohm ▸ Regenerate
UIType Class** — still clickable, because a mid-session compile error keeps the last good domain
loaded. If the Editor was *restarted* into that error it enters Safe Mode, and the only fix is
editing `UIType.g.cs` by hand.

---

## 13. Console message → cause

Every message is prefixed `OhmUI: `.

| Message | Cause / fix |
|---|---|
| `No active UI config` | No `OhmUISettings` assigned in *Project Settings ▸ Ohm UI ▸ UI Settings*. |
| `No UI prefabs registered` | The active config's **Default UI Prefabs** list is empty. |
| `UI <T> is not registered in the UIManager UI list!` | The prefab is not in the active config and not on a `UIBakedHandler`. |
| `Start UI '<X>' is not registered` | The config's **Start UI** points at a type missing from its own prefab list. |
| `Duplicate UI Type found: <X> … skipped` | Two prefabs share one `UIBase` subclass. One class = one prefab. |
| `A second UIManager was found in scene …` | Delete the scene-placed `UIManager`. |
| `UIBakedHandler … found no UIManager` | **Auto Bootstrap** is off on the active config. |
| `'<X>' is Detached and cannot be registered from a scene` | Untick **Detached**, or move the UI into the config instead of the scene. |
| `another scene instance of <T> already owns that UI type` | Two baked instances of one type. Keep one. |
| `cannot override <T> — the registered UI is Detached` | Remove the detached project-wide entry, or do not bake this type. |
| `Cannot ShowUIForDuration — '<X>' is already showing as the active UI` | Tick **Detached** on that prefab, or do not use the timed verb on a nav screen. |
| `<X> does not implement IUIInjectable<TData>` | Add `IUIInjectable<TData>` to the screen, or fix the payload type. |
| `UI <T> failed to open — its scene instance was destroyed while still registered` | Call `UnregisterInstance` before destroying a baked UI. |
| `UI <T> failed to open. Instance is null and no Prefab is assigned!` | The config list holds a null / missing entry. |
| `Cannot CloseUI — prefab '<X>' is not registered` | Close by `Type`, or register the prefab. |
| `UIStarter could not find UIManager.Instance` | **Auto Bootstrap** is off, or no active config is assigned. |

---

## 14. Anti-patterns

| Don't | Do |
|---|---|
| `Instantiate(prefab, UIManager.Instance.Parent)` | `UIManager.Instance.ShowUI<T>()` |
| `screen.Show()` on a managed screen | `ShowUI<T>()` — a direct `Show` skips layer and history bookkeeping |
| `ShowUI(prefab)` | `ShowUI(prefab.GetType())` / `ShowUI<T>()` / `ShowUI(UIType.X)` |
| `detachedInstance.gameObject.SetActive(false)` | `detachedInstance.Hide()` — recycles into the pool |
| `ShowUI(type, true)` positionally | `ShowUI(type, recordInHistory: true)` — pass the flags by name |
| `StartCoroutine` to hide a toast after N seconds | `ShowUIForDuration<T>(N)` |
| `public override void Hide(bool i = false) { /* no base */ }` | Call `base.Hide(i)` |
| Dropping a screen into a scene canvas "so it exists" | Register it in the config, or bake it via `UIBakedHandler` |
| Editing `UIType.g.cs` to add a screen | Add the script; codegen picks it up on the next reload |

Null-check `UIManager.Instance` in `OnDestroy` / `OnApplicationQuit` — teardown order is undefined
and the manager may already be gone.

---

## 15. Removed API — do not emit

Deleted in v6.0 (navigation-graph removal) and v5.0 (API cleanup). If you see these in older code or
older docs, they no longer compile:

`UINavGraph` · `UIScreenNav` · `UINavLink` · `UINavRetarget` · `NavigationAction` ·
`UINavButtonList` / `NavButton` · `UINavigationBinder` · `UICondition` (+ `And` / `Or` / `Not` /
`DelegateCondition`) · `UIConditions` · `[UINavCondition]` · `UINavConditionRegistry` ·
`UIManager.GetNavData` · `UIBase.RefreshNavigationConditions` / `AddNavListener` / `AddNavCondition` /
`BindNavigation` · `UIManager.ChangeUI` (→ `ShowUI`) · the `isBackwards` parameter (back steps are
auto-detected) · every prefab-typed `ShowUI` / `ShowUIForDuration` / `OnEscape` overload ·
`Window ▸ OhmUI ▸ Navigation Graph` · *Generate Baked UIs* / *Generate Nav Graph* on the `UIManager`
· *Project Settings ▸ Ohm UI ▸ General*.

Replacements: wire `Button.onClick` in the screen's own `UIBase` subclass; gate buttons with
`button.interactable`; set a fixed back target with **Is Back To Previous UI** + **Back To** on the
prefab.

---

## 16. Editor entry points

| Path | Purpose |
|---|---|
| *Project Settings ▸ Ohm UI ▸ UI Settings* | Active config picker, **Create New Config…**, **Select Config**, and the project-wide **Layers** list (**Save & Regenerate**). |
| *Assets ▸ Create ▸ OhmUI ▸ UI Script* / *(Injectable)* | New `UIBase` subclass, named inline. |
| `UIBakedHandler` inspector | **Bake Into Scene** plus validation warnings. |
| `UIBase` inspector | Detached / Pooled explainers, **Show (Instant)** / **Hide (Instant)** preview. |
| `TransitionController` inspector | **Fetch Transitions (Include Children)**, **Capture All Show/Hide Configs**, preview buttons. |
| `UIManager` inspector (Play Mode) | **Runtime Layer State** and **Registered UIs**. |
| *Tools ▸ Ohm ▸ Regenerate UIType Class* | Rebuild `UIType` after an out-of-band script delete or rename. |
| *Tools ▸ Ohm ▸ Strip Missing Nav Scripts (All Screen Prefabs)* | One-shot cleanup of orphaned pre-v6 components. |
