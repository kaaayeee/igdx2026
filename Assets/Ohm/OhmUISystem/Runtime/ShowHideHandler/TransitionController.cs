using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ohm.UISystem
{
    [System.Serializable]
    public class TransitionSetup
    {
        public TransitionBase transition;
        public float showDelay = 0f;
        public float hideDelay = 0f;
    }

    public class TransitionController : MonoBehaviour, IDisableHandler
    {
        [Header("Controller Settings")]
        [SerializeField] private bool showOnEnable = false;
        [SerializeField] private bool disableAfterHide = true;

        [SerializeField] private List<TransitionSetup> transitions = new();

        private Coroutine hideWaitCoroutine;
        private List<Coroutine> activeCoroutines = new();

        public float Duration => GetShowDuration();

        /// <summary>The configured transition setups (read-only; used by the editor for capture/Undo).</summary>
        public IReadOnlyList<TransitionSetup> Transitions => transitions;

        public float GetShowDuration()
        {
            float longest = 0f;
            foreach (var setup in transitions)
            {
                if (setup.transition != null)
                {
                    float finishTime = setup.showDelay + setup.transition.GetDuration(true);
                    if (finishTime > longest) longest = finishTime;
                }
            }
            return longest;
        }

        public float GetHideDuration()
        {
            float longest = 0f;
            foreach (var setup in transitions)
            {
                if (setup.transition != null)
                {
                    float finishTime = setup.hideDelay + setup.transition.GetDuration(false);
                    if (finishTime > longest) longest = finishTime;
                }
            }
            return longest;
        }

        private void OnEnable()
        {
            if (showOnEnable)
                Show(instant: true);
        }

        public void HandleDisable()
        {
            Hide();
        }

        private void StopAllActiveCoroutines()
        {
            foreach (var c in activeCoroutines)
            {
                if (c != null) StopCoroutine(c);
            }
            activeCoroutines.Clear();
        }

        public void Show(bool instant = false)
        {
            StopAllActiveCoroutines();
            if (hideWaitCoroutine != null)
            {
                StopCoroutine(hideWaitCoroutine);
                hideWaitCoroutine = null;
            }

            gameObject.SetActive(true);

            foreach (var setup in transitions)
            {
                if (setup.transition != null)
                {
                    if (instant || setup.showDelay <= 0f)
                    {
                        setup.transition.TriggerShow(instant);
                    }
                    else
                    {
                        activeCoroutines.Add(StartCoroutine(DelayTransitionCoroutine(setup.transition, setup.showDelay, true)));
                    }
                }
            }
            Debug.Log("Show Transitions: " + gameObject.name + " instant: " + instant);
        }

        public void Hide(bool instant = false)
        {
            if (!gameObject.activeInHierarchy) return;

            StopAllActiveCoroutines();
            if (hideWaitCoroutine != null)
            {
                StopCoroutine(hideWaitCoroutine);
                hideWaitCoroutine = null;
            }

            foreach (var setup in transitions)
            {
                if (setup.transition != null)
                {
                    if (instant || setup.hideDelay <= 0f)
                    {
                        setup.transition.TriggerHide(instant);
                    }
                    else
                    {
                        activeCoroutines.Add(StartCoroutine(DelayTransitionCoroutine(setup.transition, setup.hideDelay, false)));
                    }
                }
            }

            if (disableAfterHide)
            {
                if (instant)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    hideWaitCoroutine = StartCoroutine(WaitAndDisable(GetHideDuration()));
                }
            }
        }

        private IEnumerator DelayTransitionCoroutine(TransitionBase trans, float delay, bool isShow)
        {
            if (trans.RunWhilePaused)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return new WaitForSeconds(delay);

            if (isShow)
                trans.TriggerShow(false);
            else
                trans.TriggerHide(false);
        }

        private IEnumerator WaitAndDisable(float time)
        {
            bool useUnscaled = transitions.Count > 0 && transitions[0] != null && transitions[0].transition != null && transitions[0].transition.RunWhilePaused;
            
            if (useUnscaled)
                yield return new WaitForSecondsRealtime(time);
            else
                yield return new WaitForSeconds(time);

            gameObject.SetActive(false);
            hideWaitCoroutine = null;
        }

        [ContextMenu("Fetch Transitions")]
        public void FetchTransitions()
        {
            transitions.Clear();
            var foundTransitions = GetComponentsInChildren<TransitionBase>(true);
            foreach (var trans in foundTransitions)
            {
                transitions.Add(new TransitionSetup { transition = trans, showDelay = 0f, hideDelay = 0f });
            }
        }

        /// <summary>Captures the current live state of every capture-supporting transition into its show config.</summary>
        public void CaptureAllShowConfigs()
        {
            foreach (var setup in transitions)
            {
                if (setup.transition != null && setup.transition.SupportsCapture)
                    setup.transition.CaptureShowConfig();
            }
        }

        /// <summary>Captures the current live state of every capture-supporting transition into its hide config.</summary>
        public void CaptureAllHideConfigs()
        {
            foreach (var setup in transitions)
            {
                if (setup.transition != null && setup.transition.SupportsCapture)
                    setup.transition.CaptureHideConfig();
            }
        }

        [ContextMenu("Auto-Reverse Hide Delays")]
        public void AutoReverseHideDelays()
        {
            float maxShowDelay = 0f;
            foreach (var setup in transitions)
            {
                if (setup != null && setup.showDelay > maxShowDelay)
                    maxShowDelay = setup.showDelay;
            }

            foreach (var setup in transitions)
            {
                if (setup != null)
                {
                    setup.hideDelay = maxShowDelay - setup.showDelay;
                }
            }
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}
