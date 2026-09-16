using UnityEngine;

namespace Ohm.UISystem
{
    public abstract class TransitionBase : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] protected bool disableAfterHide = false;
        [SerializeField] protected bool runWhilePaused = true;
        public bool RunWhilePaused => runWhilePaused;
        
        public abstract float GetDuration(bool isShow);

        public void TriggerShow(bool instant = false)
        {
            if (!instant) PrepareShow();
            PlayShow(instant);
        }

        public void TriggerHide(bool instant = false) => PlayHide(instant);

        public abstract void PrepareShow();

        protected abstract void PlayShow(bool instant);
        protected abstract void PlayHide(bool instant);

        // --- Edit-time config capture (overridden by subclasses that expose a capturable value) ---

        /// <summary>Whether this transition supports capturing the current live state into its show/hide config.</summary>
        public virtual bool SupportsCapture => false;

        /// <summary>Writes the object's current live value into the show config.</summary>
        public virtual void CaptureShowConfig() { }

        /// <summary>Writes the object's current live value into the hide config.</summary>
        public virtual void CaptureHideConfig() { }
    }
}
