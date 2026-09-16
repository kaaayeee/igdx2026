using System.Collections;
using UnityEngine;

namespace Ohm.UISystem.Transitions
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorTransition : TransitionBase
    {
        [System.Serializable]
        public struct AnimatorConfig
        {
            public string stateName;
            public float duration;
        }

        [SerializeField] private AnimatorConfig showConfig = new AnimatorConfig { stateName = "Show", duration = 0.5f };
        [SerializeField] private AnimatorConfig hideConfig = new AnimatorConfig { stateName = "Hide", duration = 0.5f };

        private Animator animator;
        private Coroutine disableRoutine;

        public override float GetDuration(bool isShow) => isShow ? showConfig.duration : hideConfig.duration;

        private void Awake() => animator = GetComponent<Animator>();

        public override void PrepareShow()
        {
            // Untuk Animator, prepare diabaikan karena Play akan melompat langsung
        }

        protected override void PlayShow(bool instant)
        {
            StopDisableRoutine();
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (animator == null) animator = GetComponent<Animator>();

            if (animator != null && !string.IsNullOrEmpty(showConfig.stateName))
            {
                animator.Play(showConfig.stateName, -1, instant ? 1f : 0f);
            }
        }

        protected override void PlayHide(bool instant)
        {
            StopDisableRoutine();

            if (animator == null) animator = GetComponent<Animator>();

            if (animator != null && !string.IsNullOrEmpty(hideConfig.stateName))
            {
                animator.Play(hideConfig.stateName, -1, instant ? 1f : 0f);
            }

            if (!disableAfterHide) return;

            if (instant || !gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            disableRoutine = StartCoroutine(DisableAfterDelay(hideConfig.duration));
        }

        private void StopDisableRoutine()
        {
            if (disableRoutine == null) return;

            StopCoroutine(disableRoutine);
            disableRoutine = null;
        }

        private IEnumerator DisableAfterDelay(float delay)
        {
            if (runWhilePaused)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return new WaitForSeconds(delay);

            disableRoutine = null;
            gameObject.SetActive(false);
        }
    }
}
