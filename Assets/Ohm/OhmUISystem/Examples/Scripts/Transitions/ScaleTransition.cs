using UnityEngine;
using DG.Tweening;

namespace Ohm.UISystem.Transitions
{
    public class ScaleTransition : TransitionBase
    {
        [System.Serializable]
        public struct ScaleConfig
        {
            public Vector3 targetScale;
            public float duration;
            public Ease ease;
        }

        [SerializeField] private ScaleConfig showConfig = new(){ targetScale = Vector3.one, duration = 0.5f, ease = Ease.OutBack };
        [SerializeField] private ScaleConfig hideConfig = new(){ targetScale = Vector3.zero, duration = 0.5f, ease = Ease.InBack };

        private Tween scaleTween;

        public override float GetDuration(bool isShow) => isShow ? showConfig.duration : hideConfig.duration;

        public override bool SupportsCapture => true;

        public override void CaptureShowConfig() => showConfig.targetScale = transform.localScale;

        public override void CaptureHideConfig() => hideConfig.targetScale = transform.localScale;

        public override void PrepareShow()
        {
            transform.localScale = hideConfig.targetScale;
        }

        protected override void PlayShow(bool instant)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            scaleTween?.Kill();

            if (instant)
            {
                transform.localScale = showConfig.targetScale;
            }
            else
            {
                transform.localScale = hideConfig.targetScale;
                scaleTween = transform.DOScale(showConfig.targetScale, showConfig.duration)
                    .SetEase(showConfig.ease)
                    .SetUpdate(runWhilePaused);
            }
        }

        protected override void PlayHide(bool instant)
        {
            scaleTween?.Kill();

            if (instant)
            {
                transform.localScale = hideConfig.targetScale;
                if (disableAfterHide) gameObject.SetActive(false);
            }
            else
            {
                scaleTween = transform.DOScale(hideConfig.targetScale, hideConfig.duration)
                    .SetEase(hideConfig.ease)
                    .SetUpdate(runWhilePaused)
                    .OnComplete(() => { if (disableAfterHide) gameObject.SetActive(false); });
            }
        }
    }
}
