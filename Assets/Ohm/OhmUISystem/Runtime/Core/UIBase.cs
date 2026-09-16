using UnityEngine;
using UnityEngine.UI;

namespace Ohm.UISystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIBase : MonoBehaviour
    {
        [Header("UI Settings")]
        [Tooltip("Which layer this screen belongs to")]
        [SerializeField] private UILayer layer = UILayer.Main;

        [Tooltip("When to instantiate this screen: up front on startup, or on first show. " +
                 "To bake it into a scene instead, list the scene instance on that scene's UIBakedHandler — " +
                 "this setting is then ignored, because the instance already exists.")]
        [SerializeField] private SpawnBehavior spawnBehavior = SpawnBehavior.LazyLoad;

        [Tooltip("Screen-specific configuration applied when shown (pause, input, ...)")]
        [SerializeField] private UIConfigData configData;

        [Tooltip("Record THIS UI on its layer's back history when something else later replaces it — " +
                 "it is read when navigating away, not when this UI is shown. Untick for popups/confirmations " +
                 "that back navigation should skip over. Same-layer only: opening a UI on a different layer " +
                 "never consults this, because each layer owns its own history stack. " +
                 "ShowUI's recordInHistory parameter overrides this per call.")]
        [SerializeField] private bool recordInHistory = true;

        [Tooltip("Back navigation pops this layer's history stack. Untick to always return to one fixed UI instead.")]
        [SerializeField] private bool isBackToPreviousUI = true;

        [Tooltip("UI prefab to return to when Is Back To Previous UI is off. Must be registered in the UIManager's UI list.")]
        [SerializeField, SerializeFieldIf("isBackToPreviousUI", invert: true)] private UIBase backTo;

        [Tooltip("Detached UIs are excluded from the UIManager navigation system: no history, no back navigation, and no auto-hide when a lower layer opens. You must hide them manually (instance.Hide() or UIManager.CloseUI(instance)); they are only auto-closed by CloseLayer/CloseAllUI.")]
        [SerializeField] private bool detached = false;

        [Tooltip("Allow multiple instances of this UI at once, recycled through an object pool. Requires Detached.")]
        [SerializeField, ReadOnlyIf("detached", invert: true)] private bool pooled = false;

        [Tooltip("Instances to prewarm into the pool, and — with Dynamic Pooling off — the maximum live at once.")]
        [SerializeField, SerializeFieldIf("pooled"), ReadOnlyIf("detached", invert: true)] private int poolSize = 4;

        [Tooltip("Let the pool instantiate past Pool Size on demand and keep the extras. Off: Pool Size is a hard cap — a show past it recycles the oldest showing instance.")]
        [SerializeField, SerializeFieldIf("pooled"), ReadOnlyIf("detached", invert: true)] private bool dynamicPooling = true;

        [Header("Presentation")]
        [SerializeField] private TransitionController transitionController;
        [SerializeField] private Button firstSelected;
        [ReadOnly] public bool isActive;

        public UILayer Layer => layer;
        public SpawnBehavior SpawnBehavior => spawnBehavior;
        public UIConfigData ConfigData => configData;
        public bool RecordInHistory => recordInHistory;
        public bool IsBackToPreviousUI => isBackToPreviousUI;
        public UIBase BackTo => backTo;
        public bool Detached => detached;
        public bool Pooled => pooled;

        /// <summary>Clamped here rather than via [Min] — Unity runs a single PropertyDrawer per field, which SerializeFieldIf already occupies.</summary>
        public int PoolSize => Mathf.Max(1, poolSize);
        public bool DynamicPooling => dynamicPooling;

        /// <summary>Raised at the end of Hide(). UIManager uses this to recycle detached instances into their pool.</summary>
        public event System.Action<UIBase> Hidden;

        private void OnValidate()
        {
            if (!detached) pooled = false;

            // Prefabs authored before BakeInEditor was removed still serialize its old value (0),
            // which is no longer a member. Baking is a UIBakedHandler's job now, so fall back to lazy.
            if (!System.Enum.IsDefined(typeof(SpawnBehavior), spawnBehavior))
                spawnBehavior = SpawnBehavior.LazyLoad;
        }

        public TransitionController TransitionController => transitionController;
        public virtual float AnimationDuration => transitionController != null ? transitionController.Duration : 0f;
        public virtual float ShowAnimationDuration => transitionController != null ? transitionController.GetShowDuration() : 0f;
        public virtual float HideAnimationDuration => transitionController != null ? transitionController.GetHideDuration() : 0f;

        public virtual void Show(bool instant = false)
        {
            isActive = true;
            gameObject.SetActive(true);

            if(transitionController != null) transitionController.Show(instant);
        }

        public virtual void Hide(bool instant = false)
        {
            if (transitionController != null) transitionController.Hide(instant);
            else gameObject.SetActive(false);

            isActive = false;

            Hidden?.Invoke(this);
        }

        /// <summary>Subclasses overriding OnDestroy must call base.OnDestroy().</summary>
        protected virtual void OnDestroy()
        {
        }
    }
}
