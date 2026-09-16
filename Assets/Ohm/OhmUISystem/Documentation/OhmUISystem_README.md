# 🎨 Ohm UI System — Layered UI Navigation for Unity

A complete UI navigation and lifecycle framework for Unity (uGUI). Your screens stay simple
prefabs; the `UIManager` owns layers, history, pooling, and transitions, and you move between
screens with one line of code.

## 🌟 Overview

**How it works in one sentence:** every screen is a prefab with a `UIBase` component, the
`UIManager` keeps a list of those prefabs, and `UIManager.Instance.ShowUI<UIShop>()` puts one on
screen with the right layer, history, and transition handling.

**Core advantages:**

- **The prefab IS the screen** — layer, spawn behavior, pause settings, history and back rules,
  and pooling all live directly on the screen's `UIBase` component. No config asset per screen.
- **Layered UI** — priority-ordered layers (Main, Popup, ...) with automatic hiding rules and
  per-layer history stacks for back navigation.
- **Back navigation that just works** — a per-layer history stack, automatic back-step detection,
  and an optional fixed **Back To** target per screen.
- **Type-safe code API** — `UIManager.Instance.ShowUI<UIShop>()`, `ShowUI(UIType.UIShop)`, data
  injection, timed UIs, detached/pooled overlays.
- **Integrated transitions** — optional Show/Hide animation system (Fade, Scale, Slide, Move,
  Animator examples included, DOTween-based) via a `TransitionController`.

> **Dependency note:** the included example transitions use **DOTween**. The core navigation
> system itself has no third-party dependencies.

> **Using an AI assistant?** Point it at [`AGENTS.md`](../AGENTS.md) in the plugin root — a
> condensed, rules-first version of this guide written for coding agents.

> **v6.0 breaking change:** the `UINavGraph` asset, `UINavButtonList`, `UINavLink`, the
> Navigation Graph window, and the whole `UICondition` / `[UINavCondition]` system were
> **removed**. Buttons are now wired with plain `Button.onClick` calling `ShowUI(…)`, and the
> explicit back target moved onto the `UIBase` prefab. See
> [Technical_Documentation.md §5](Technical_Documentation.md#5-migration) for the migration table.

---

## 📑 Table of Contents

- [🚀 Quick Start (5 Minutes)](#-quick-start-5-minutes)
- [🏗️ Scene-Specific UIs (`UIBakedHandler`)](#-scene-specific-uis-uibakedhandler)
- [🧠 Core Concepts](#-core-concepts)
- [🖼️ Creating a UI Screen (Full Walkthrough)](#-creating-a-ui-screen-full-walkthrough)
- [📚 Layers & Back Navigation](#-layers--back-navigation)
- [💻 Code API (Programmatic Guide)](#-code-api-programmatic-guide)
- [🎬 Transition System](#-transition-system)
- [🛠️ Editor Tools Reference](#-editor-tools-reference)
- [🧪 Examples](#-examples)
- [❓ FAQ & Troubleshooting](#-faq--troubleshooting)

---

## 🚀 Quick Start (5 Minutes)

### 1. Set up the scene
Nothing to do. The `UIManager` is created in code in `DontDestroyOnLoad` before the first scene
loads, together with its canvas, layer roots, `UIConfigHandler`, and an `EventSystem` if your scene
doesn't already have one.

> Do **not** put a `UIManager` in a scene — there is exactly one, it is global, and there is no
> manager prefab to drag in. The inspector tells you off if it finds one in a scene.
>
> **The canvas is yours.** The only prefab involved is **Canvas Prefab** on the config
> (`Prefabs/UICanvas.prefab` by default) — edit it, or point the field at your own to change render
> mode, `CanvasScaler` resolution, or sorting order. Leave it empty and a Screen Space – Overlay
> canvas scaled to 1920×1080 is built in code.
>
> To turn the automatic setup off, untick **Auto Bootstrap** in
> *Project Settings ▸ Ohm UI ▸ UI Settings*.

### 2. Create a screen
1. Build your panel with normal uGUI objects, then create a small script — right-click in the
   Project window and pick **Create > OhmUI > UI Script**, then type the name (`UIShop`):

```csharp
using Ohm.UISystem;

public class UIShop : UIBase
{
}
```

2. Attach `UIShop` to the panel's root and turn it into a **prefab**.
3. In the `UIBase` inspector, pick a **Layer** (e.g. `Main`) and a **Spawn Behavior**.

### 3. Register it
Open *Project Settings ▸ Ohm UI ▸ UI Settings* and add your prefab to the **Default UI Prefabs**
list. That is all a screen needs to be reachable, from any scene.

### 4. Wire its buttons
Serialize the buttons on your subclass and hook them up:

```csharp
using UnityEngine.UI;
using Ohm.UISystem;

public class UIShop : UIBase
{
    [SerializeField] private Button buyButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        buyButton.onClick.AddListener(() => UIManager.Instance.ShowUI<UICheckout>());
        backButton.onClick.AddListener(() => UIManager.Instance.OnEscape());
    }
}
```

### 5. Play
Set **Start UI** on that same settings asset for the screen that opens on launch,
press Play, and click through your flow. For a *per-scene* start screen, drop a **`UIStarter`**
into that scene instead.

---

## 🏗️ Scene-Specific UIs (`UIBakedHandler`)

A prefab cannot reference a GameObject in a scene — Unity forbids it. So when a screen needs a
**hard reference to something in the scene** (a spawn point, a boss transform, a level-specific
camera), the screen has to *live* in that scene.

1. Add an empty GameObject under your scene's Canvas and put a **`UIBakedHandler`** on it.
2. Use its **Bake Into Scene** button to drop a UI prefab in as a scene instance, or drag an
   existing scene UI into the **Baked UIs** list.
3. Wire its scene references normally in the inspector.

At runtime the handler registers those instances with the global `UIManager`, so they behave like
any other screen — same layers, same history, same `ShowUI` / `OnEscape` — and they are
unregistered and destroyed when the scene unloads.

**Rules:**

- List **scene instances**, never prefab assets. The inspector flags it if you get this wrong.
- One UI type maps to one entry. If a baked `UISettings` and a project-wide `UISettings` both
  exist, the scene one wins while that scene is loaded, and the project-wide one comes back after
  it unloads.
- A baked UI cannot be **Detached** — that path pools and clones its source, which for a scene
  instance would clone its scene references. Put pooled UIs in the UI Settings config instead.
- Keep the scene canvas's `CanvasScaler` matching the global one. Baked UIs are re-parented under
  the global canvas at runtime, so a mismatch shifts the layout between edit mode and play mode.
  The inspector warns about this too.

---

## 🧠 Core Concepts

| Piece | What it is |
|---|---|
| `UIManager` | Global singleton, created in code before the first scene loads. Owns the layers, the history, and every registered UI. Entry point for all code calls (`UIManager.Instance`). The list of screen prefabs lives on the active `OhmUISettings` config. |
| `UIBase` | Base class for every screen prefab. Carries the screen's own settings (layer, spawn behavior, config data, history/back rules, pooling) and its Show/Hide lifecycle. |
| `UILayer` | Auto-generated enum of your UI layers (Main, Popup, ...). Order = priority. |
| `UIType` | Auto-generated type reference per `UIBase` subclass — `ShowUI(UIType.UIShop)`. |
| `TransitionController` | Optional component orchestrating Show/Hide animations for a screen. |

**How it fits together:**

```
OhmUISettings  ──> defaultUIPrefabs        "which screens exist"
UIBakedHandler ──> scene instances         "...plus this scene's own"

UIManager ──> currentUIPerLayer            "what's on screen, per layer"
          ──> historyPerLayer              "how to go back, per layer"

UIBase (on the prefab)  ──> layer / spawn behavior / config / back rules / pooling
Your subclass           ──> Button.onClick → UIManager.Instance.ShowUI<T>()
```

Buttons stay inside their screen prefab and are referenced as ordinary serialized fields on the
screen's own subclass — nothing external needs to reach into the prefab.

---

## 🖼️ Creating a UI Screen (Full Walkthrough)

### 1. The script

Every screen inherits from `UIBase`. The fastest way to start one is to right-click a folder in the
Project window and choose:

| Menu item | Generates |
|---|---|
| **Create > OhmUI > UI Script** | An empty `UIBase` subclass, with the `Show` / `Hide` / `OnDestroy` overrides included as commented-out stubs. |
| **Create > OhmUI > UI Script (Injectable)** | The same, plus a `[System.Serializable]` data struct and `IUIInjectable<T>.Inject()` — for screens that receive a payload (see [Passing data to a screen](#passing-data-to-a-screen-dependency-injection)). |

Both work exactly like Unity's built-in **Create > C# Script**: type the file name inline and the
class is named to match. Scripts are generated in the global namespace.

Written by hand it's just as simple — override the lifecycle if you need custom behavior:

```csharp
using UnityEngine;
using Ohm.UISystem;

public class UIInventory : UIBase
{
    public override void Show(bool instant = false)
    {
        base.Show(instant);
        Debug.Log("Inventory opened!");
    }

    public override void Hide(bool instant = false)
    {
        base.Hide(instant);
        Debug.Log("Inventory closed!");
    }
}
```

> ⚠️ If you override `OnDestroy()`, call `base.OnDestroy()`. The base body is empty as of v6.0,
> but the virtual is kept so existing overrides keep compiling.

A screen with an empty subclass is perfectly fine — a screen that is only ever opened and closed
from elsewhere needs no code of its own.

### 2. UI Settings (on the `UIBase` component)

| Field | Meaning |
|---|---|
| **Layer** | Which `UILayer` the screen lives on (see [Layers](#-layers--back-navigation)). |
| **Spawn Behavior** | How/when the instance is created — see table below. |
| **Config Data** | Side-effects applied when the screen becomes active: `Pause Game` (sets `Time.timeScale = 0`) and `Enable Input` (a stub for you to extend in `UIConfigHandler`). |
| **Record In History** | Ticked (default): the screen is recorded in its layer's back history when you navigate away. Untick for popups/confirmations that back navigation should skip. `ShowUI`'s `recordInHistory` parameter overrides this per call. |
| **Is Back To Previous UI** | Ticked (default): `OnEscape()` from this screen pops its layer's history. Untick to always return to one fixed screen instead. |
| **Back To** | Shown only when the above is unticked — the screen to always go back to. Must be registered in the same `UIManager`. |
| **Detached** | Excludes the screen from the navigation model entirely: no history, no back, no auto-hide. You hide it yourself. |
| **Pooled** / **Pool Size** / **Dynamic Pooling** | Multiple recycled instances at once. Requires **Detached**. With Dynamic Pooling off, Pool Size is a hard cap — a show past it recycles the oldest showing instance. |

**Spawn Behavior options:**

| Value | Meaning |
|---|---|
| `PrewarmOnAwake` | Instantiated hidden during `UIManager` initialization. |
| `LazyLoad` | Instantiated the first time it is shown. Cheapest startup, small hitch on first open. |

Both end up parented under their layer's root object (`Layer_<Name>`), which the manager
creates automatically.

### 3. Presentation fields

- **Transition Controller** — optional; assign one to animate Show/Hide
  (see [Transitions](#-transition-system)).
- **First Selected** — the `Button` to auto-select when the screen shows (gamepad/keyboard
  navigation).

## 📚 Layers & Back Navigation

### Layers

Layers control draw order and auto-hiding. Configure them at
**Edit → Project Settings → Ohm UI → UI Settings**, in the **Layers** section: a reorderable list of names
(default `Main`, `Popup`). Index 0 = lowest priority (background), last = highest (overlay).
Click **"Save & Regenerate"** — this regenerates the `UILayer` enum, so all layer dropdowns
update project-wide.

Runtime rules:

- The manager creates one root object per layer (`Layer_Main`, `Layer_Popup`, ...) and keeps
  them sibling-ordered, so higher layers always render on top.
- Showing a UI **hides everything on higher layers** (opening a Main screen closes popups) and
  replaces the current UI on its own layer.
- Each layer keeps its own **history stack** for back navigation.

In Play Mode, the `UIManager` inspector shows a live **Runtime Layer State** panel (active UI +
history count per layer).

### Back navigation

Call `UIManager.Instance.OnEscape()` from your input handling (e.g. the Escape key or gamepad B)
to navigate back from the topmost active screen. Two fields on the screen's `UIBase` decide what
"back" means:

- **Is Back To Previous UI** (default) — go back through that layer's history stack.
- **Back To** — an explicit screen to always go back to instead. Assign it when the flag above is
  unticked; the target must be registered in the same `UIManager`.

Wire a Back button to `OnEscape()` and it follows those rules automatically.

From code you can also:

- `ShowPreviousUI()` — go straight back through the topmost layer's history stack
  (`ShowPreviousUI(UILayer.X)` for a specific layer); no-op when the history is empty.
- `OnEscape<UIShop>()` — trigger back navigation for a specific screen instead of the topmost
  one (no-op unless that screen is the active one on its layer).
- `ShowUI(..., recordInHistory: false)` — show a screen that the history system **skips**: it is
  never recorded, so back navigation always returns to what was shown before it. Ideal for
  information popups and confirmation dialogs. The default (parameter omitted) is the prefab's
  **Record In History** checkbox on `UIBase`, so a popup can be marked history-skipped once in
  the inspector instead of at every call site; `recordInHistory: true/false` overrides the
  checkbox per call.

**Back steps are automatic.** Showing a screen that is already the top of its layer's history
**pops** it rather than pushing the current screen — so a "back" button wired as a plain
`ShowUI` call unwinds instead of growing the stack. You never pass a flag for this.

**Two things to know about `Record In History`,** because they trip people up:

1. It is read when navigating **away** from that screen, not when it is shown. Ticking it off on
   screen X means "when something replaces X, don't remember X" — it does **not** stop the screen
   that was showing *before* X from being recorded.
2. It is **same-layer only**. Each layer owns its own history stack, so opening a screen on a
   *different* layer never consults the flag. And when a layer's history is empty, `OnEscape`
   **closes the whole layer** rather than popping — which is why a popup chain with no recorded
   entries falls through to whatever sits underneath.

So for `Gameplay → PauseMenu → Settings` (with PauseMenu and Settings both on the Popup layer),
tick **Record In History** *off on `Settings`* — not on `PauseMenu`. Backing out of Settings then
returns to the pause menu, and backing out again closes the Popup layer to reveal Gameplay.

---

## 💻 Code API (Programmatic Guide)

Everything goes through the singleton `UIManager.Instance` (namespace `Ohm.UISystem`).
Any registered screen is fully drivable from code.

### Showing screens

```csharp
// By type (generic)
UIManager.Instance.ShowUI<UIInventory>();

// By generated type reference (see UIType below)
UIManager.Instance.ShowUI(UIType.UIInventory);

// From a prefab reference (e.g. a [SerializeField] UIBase field) — pass its type
UIManager.Instance.ShowUI(inventoryPrefab.GetType());

// Show without recording it in back history — for info popups / confirmations.
// Back navigation will skip this screen entirely. (Omit the parameter to use the
// prefab's "Record In History" checkbox instead — untick it there to get this
// behavior without passing anything.)
UIManager.Instance.ShowUI<UIInformation>(recordInHistory: false);

// Skip transitions. showInstant = the incoming screen; hideCurrentInstant =
// everything this call dismisses (the outgoing screen and any layers above it).
UIManager.Instance.ShowUI<UIMainMenu>(showInstant: true, hideCurrentInstant: true);

// Every ShowUI overload returns the shown instance (null on failure) — handy for
// detached UIs you need to hide yourself:
UIBase toast = UIManager.Instance.ShowUI<UIToast>();
```

### Detached / pooled UIs

Tick **Detached** on a `UIBase` prefab to take it *out* of the navigation model: it is not
recorded in history, is not reachable by back navigation, and is **not** auto-hidden when a
lower layer opens. A detached UI must be hidden **by you** — it is only auto-closed by
`CloseLayer` / `CloseAllUI`. Also tick **Pooled** (only meaningful with Detached) to allow
several instances at once, recycled through an object pool.

```csharp
// Detached + Pooled: spawn several at once, hold each reference, hide when done.
UIBase a = UIManager.Instance.ShowUI<UIToast>();
UIBase b = UIManager.Instance.ShowUI<UIToast>();   // second live instance
a.Hide();                                          // auto-returns 'a' to the pool
UIManager.Instance.CloseUI(b);                     // same effect for 'b'

// Close everything on a layer (managed screen + all detached UIs on it):
UIManager.Instance.CloseLayer(UILayer.Popup);
```

Detached UIs deliberately skip `UIConfigData` side-effects (e.g. pause) — those belong to the
navigation system they opt out of.

### Timed UIs (auto-hide after N seconds)

`ShowUIForDuration` shows a UI and hides it automatically once the timer runs out — the natural
shape for toasts, pickup notifications and "Saved!" banners, with no coroutine at the call site.

```csharp
// Show for 2 seconds, then it hides itself.
UIManager.Instance.ShowUIForDuration<UIToast>(2f);

// Same overloads as ShowUI — by type reference, and with injected data.
UIManager.Instance.ShowUIForDuration(UIType.UIToast, 2f);
UIManager.Instance.ShowUIForDuration<ToastData>(UIType.UIToast, new ToastData("Saved!"), 2f);
UIManager.Instance.ShowUIForDuration<UIToast, ToastData>(new ToastData("Saved!"), 2f);

// It still returns the instance, so you can dismiss it early — the timer is cancelled for you.
UIBase toast = UIManager.Instance.ShowUIForDuration<UIToast>(5f);
UIManager.Instance.CloseUI(toast);
```

Things to know:

- **A timed UI is always shown detached**, whether or not the prefab's **Detached** box is ticked.
  It never touches back history, never hides the screen underneath, and never applies
  `UIConfigData`. There is no `recordInHistory` or instant parameter because none of them apply.
- **The timer is unscaled**, so a timed UI still dismisses itself while the game is paused
  (`Time.timeScale = 0`).
- **Showing the same UI again restarts its countdown** rather than stacking a second timer.
- **Dismissing it early cancels the timer** — via `Hide()`, `CloseUI`, `CloseLayer` or
  `CloseAllUI` — so a recycled pooled instance is never hidden by a stale timer.
- **Tick Detached (and Pooled, for several at once) on prefabs you intend to use this way.** On a
  prefab that is *not* Detached and has a baked or pre-warmed instance, the first timed show
  creates a separate pooled instance, because the baked one stays reserved for the navigation
  model. Calling it on a UI that is *currently the active screen on its layer* is refused with a
  warning, since it would put two copies on screen.

### Passing data to a screen (dependency injection)

Implement `IUIInjectable<TData>` on the screen and use the data overload:

```csharp
public struct ConfirmData { public string title; public System.Action onConfirm; }

public class UIConfirmation : UIBase, IUIInjectable<ConfirmData>
{
    public void Inject(ConfirmData data) { /* apply title, callbacks... */ }
}

// Caller — by generic type, checked at compile time:
UIManager.Instance.ShowUI<UIConfirmation, ConfirmData>(
    new ConfirmData { title = "Quit?", onConfirm = QuitGame });

// ...or by Type, when the target is only known at runtime:
UIManager.Instance.ShowUI(UIType.UIConfirmation,
    new ConfirmData { title = "Quit?", onConfirm = QuitGame });
```

The two-generic form (`ShowUI<TUI, TData>`) pairs the screen and its payload at compile time, so a
mismatched data type is a build error rather than a runtime warning. The `Type` form stays available
for dynamic targets and the generated `UIType.X` constants. `ShowUIForDuration` mirrors both shapes.

See `Examples/Scripts/Menus/UIConfirmation.cs` for a complete reusable dialog built this way.
The data overloads also accept `recordInHistory: false` (or untick **Record In History** on the
prefab) — ideal for a confirmation dialog that back navigation should never return to.

### Back / closing

```csharp
UIManager.Instance.OnEscape();             // back from the topmost active screen
UIManager.Instance.OnEscape<UIShop>();     // back from a specific screen (only if it is the active one on its layer)
UIManager.Instance.ShowPreviousUI();       // go back through the topmost layer's history
UIManager.Instance.ShowPreviousUI(UILayer.Main); // go back on a specific layer
UIManager.Instance.CloseUI<UIPauseMenu>(); // hide a specific screen (no history restore)
UIManager.Instance.CloseUI(toastInstance); // hide a checked-out detached instance (recycles it)
UIManager.Instance.CloseLayer(UILayer.Popup); // close a whole layer (managed screen + detached UIs on it)
UIManager.Instance.CloseAllUI();           // hide everything, clear all history, close all detached UIs

// Every close/back API takes an optional `instant` to skip the hide transition:
UIManager.Instance.CloseAllUI(instant: true);
UIManager.Instance.OnEscape(instant: true);
```

`OnEscape` follows the screen's back rule on its `UIBase` (history or an explicit **Back To**
screen), while `ShowPreviousUI` always walks the history stack directly. `CloseUI` just dismisses:
the screen is hidden, its layer becomes empty, and that layer's history is cleared — nothing is
restored.

### Queries & events

```csharp
bool open = UIManager.Instance.IsUIActive<UIInventory>();

System.Type current = UIManager.Instance.CurrentUI;   // topmost active screen type
System.Type onMain  = UIManager.Instance.GetCurrentUI(UILayer.Main);
int depth           = UIManager.Instance.GetHistoryCount(UILayer.Main);

UIManager.Instance.OnUIChanged += type => Debug.Log($"UI changed to {type}");
```

### `UIType` — generated type references

`UIType.g.cs` is auto-generated on every script reload with one entry per `UIBase` subclass
(e.g. `UIType.UIMainMenu`). It's the convenient way to reference screens in code without a
prefab field. **Never edit generated files** (`UIType.g.cs`, `UILayer.g.cs`) by hand.

Deleting a screen script from the Project window removes its `UIType` entry automatically. The one
case that needs a nudge is renaming a screen class *inside* its file, or deleting a script outside
Unity — the console then reports a missing type in `UIType.g.cs`. Run
**Tools > Ohm > Regenerate UIType Class** to fix it.

---

## 🎬 Transition System

Optional Show/Hide animations per screen:

1. Add transition components to the objects you want to animate (`FadeTransition`,
   `ScaleTransition`, `SlideTransition`, `MoveTransition`, `AnimatorTransition` — all included
   under `Examples/Scripts/Transitions/`).
2. Add a **`TransitionController`** to the screen and assign it to the `UIBase`'s
   **Transition Controller** field.
3. Click **"Fetch Transitions (Include Children)"** in the `TransitionController` inspector to
   collect all child transitions automatically.
4. Per entry, tune **Show Delay** / **Hide Delay** to stagger elements.
   **"Auto-Reverse Hide Delays"** (right-click the component header) mirrors your show order for
   hiding.

Controller options: **Show On Enable** (snap to the shown state whenever the object is enabled —
instant, no animation)
and **Disable After Hide** (deactivate the GameObject once the hide animation finishes).

**Included transitions:** Fade (CanvasGroup alpha), Scale (localScale), Slide (from/to an
off-screen direction), Move (to a position/transform), Animator (plays named "Show"/"Hide"
states). Each has independent Show and Hide configs (target values, duration, easing) and a
**Run While Paused** toggle so menus can animate while `Time.timeScale = 0`.

### Custom transitions

Inherit from `TransitionBase` and implement all four members:

```csharp
using Ohm.UISystem;

public class RotateTransition : TransitionBase
{
    public float duration = 0.3f;

    public override float GetDuration(bool isShow) => duration;
    public override void PrepareShow() { /* set the pre-show state */ }
    protected override void PlayShow(bool instant) { /* animate in  */ }
    protected override void PlayHide(bool instant) { /* animate out */ }
}
```

---

## 🛠️ Editor Tools Reference

**On the `UIManager` inspector:**

| Tool | What it does |
|---|---|
| **Runtime Layer State** | (Play Mode) Live view of the active UI and history depth per layer. |
| **Registered UIs** | (Play Mode) Every registered UI type, and whether it comes from the project defaults or a scene. |

**On the `UIBakedHandler` inspector:**

| Tool | What it does |
|---|---|
| **Bake Into Scene** | Instantiates a UI prefab into this scene as a linked prefab instance and adds it to the Baked UIs list. |
| **Validation** | Flags prefab assets in the list, duplicate UI types, Detached UIs, and a `CanvasScaler` that doesn't match the global canvas. |

**In the Project window (right-click → Create → OhmUI):**

| Tool | What it does |
|---|---|
| **UI Script** | Creates a new `UIBase` subclass script, named inline like Unity's built-in "C# Script". |
| **UI Script (Injectable)** | Same, plus a data struct and `IUIInjectable<T>.Inject()`. |

**Tools → Ohm:**

| Tool | What it does |
|---|---|
| **Regenerate UIType Class** | Rebuilds `UIType` from the screens currently in the project. You normally never need this — it rebuilds itself after every compile, and deleting a screen script prunes it automatically. Use it if a screen was deleted outside Unity, or if you renamed a screen class inside its file and the console now complains about a missing type in `UIType.g.cs`. |
| **Strip Missing Nav Scripts** | Removes missing-script components from every screen prefab — run once after upgrading to v6.0 to clear the orphaned `UINavButtonList` components. |

**Project Settings → Ohm UI:**

- **UI Settings** — one page, two sections:
  - **UI Config** — picks the **Active Config**: the `OhmUISettings` asset holding the project-wide
    UI list, the start UI, **Auto Bootstrap**, the **Canvas Prefab**, and **Close All On Scene
    Change**. Edit those on the asset itself (**Select Config** jumps to it). **Create New Config…**
    makes another one, so you can keep a dev config, a mobile config, and so on, and swap which is
    live. Only the active config ships in a build.
  - **Layers** — add/remove/reorder layers; **Save & Regenerate** rebuilds the `UILayer` enum.
    Layers are project-wide and shared by every config, because the enum is generated at compile
    time.

---

## 🧪 Examples

Open `Examples/Scenes/SampleScene` for a fully wired setup. The example scripts each demonstrate
one pattern:

- **`UIMainMenu`** — wiring buttons from code in `Start`.
- **`UIConfirmation`** — a reusable confirm dialog using `IUIInjectable<T>` data injection.
- **`UISettings`** — navigating from code with `ShowUI(UIType.UIGameplay)` and using transitions.
- **`UIGameplay` / `UIPauseMenu`** — opening and closing a Popup-layer screen over gameplay.
- **`Transitions/`** — the five ready-made `TransitionBase` implementations.

---

## ❓ FAQ & Troubleshooting

**A button does nothing at runtime.**
Check, in order: the `Button` field is assigned on the prefab; the `onClick` listener is actually
registered (`Awake`/`Start` runs only on an active GameObject — for a `LazyLoad` screen the
instance is created on first show); the button is `interactable`; and the target screen is registered — in the
active config's **Default UI Prefabs**, or on a scene `UIBakedHandler`.

**`OnEscape()` does nothing.**
It is a no-op unless the screen is the *active* UI on its layer. If **Is Back To Previous UI** is
unticked, check that **Back To** points at a prefab registered in the same `UIManager` — an
unregistered target is silently ignored.

**My screen prefabs show "missing script" components after upgrading.**
Those are the removed `UINavButtonList` components. Run
**Tools > Ohm > Strip Missing Nav Scripts (All Screen Prefabs)** once.

**Where did the Navigation Graph go?**
Removed in v6.0, along with `UINavGraph`, `UINavButtonList`, and the `UICondition` system. Wire
buttons in your `UIBase` subclass and gate them with `button.interactable`. See
[Technical_Documentation.md §5](Technical_Documentation.md#5-migration).

**`UIType.g.cs` / `UILayer.g.cs` show as modified.**
They regenerate on every script reload — that's normal. Never hand-edit them.
