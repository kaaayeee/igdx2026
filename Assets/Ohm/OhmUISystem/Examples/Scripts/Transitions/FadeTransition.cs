using UnityEngine;
using DG.Tweening;

namespace Ohm.UISystem.Transitions
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeTransition : TransitionBase
    {
        [System.Serializable]
        public struct FadeConfig
        {
            [Range(0f, 1f)] public float targetAlpha;
            public float duration;
            public Ease ease;
        }

        [SerializeField] private FadeConfig showConfig = new(){ targetAlpha = 1f, duration = 0.5f, ease = Ease.OutQuad };
        [SerializeField] private FadeConfig hideConfig = new(){ targetAlpha = 0f, duration = 0.5f, ease = Ease.InQuad };


        private CanvasGroup cg;
        private Tween fadeTween;

        private void Awake() => EnsureCanvasGroup();

        private void EnsureCanvasGroup()
        {
            if (cg == null) cg = GetComponent<CanvasGroup>();
        }

        public override float GetDuration(bool isShow) => isShow ? showConfig.duration : hideConfig.duration;

        public override bool SupportsCapture => true;

        public override void CaptureShowConfig()
        {
            EnsureCanvasGroup();
            showConfig.targetAlpha = cg.alpha;
        }

        public override void CaptureHideConfig()
        {
            EnsureCanvasGroup();
            hideConfig.targetAlpha = cg.alpha;
        }

        public override void PrepareShow()
        {
            EnsureCanvasGroup();
            cg.alpha = hideConfig.targetAlpha;
        }

        protected override void PlayShow(bool instant)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            fadeTween?.Kill();
            EnsureCanvasGroup();

            if (instant)
            {
                cg.alpha = showConfig.targetAlpha;
            }
            else
            {
                cg.alpha = hideConfig.targetAlpha;
                fadeTween = cg.DOFade(showConfig.targetAlpha, showConfig.duration > 0 ? showConfig.duration : 0.01f)
                    .SetEase(showConfig.ease)
                    .SetUpdate(runWhilePaused);
            }
        }

        protected override void PlayHide(bool instant)
        {
            fadeTween?.Kill();
            EnsureCanvasGroup();

            if (instant)
            {
                cg.alpha = hideConfig.targetAlpha;
                if (disableAfterHide) gameObject.SetActive(false);
            }
            else
            {
                fadeTween = cg.DOFade(hideConfig.targetAlpha, hideConfig.duration > 0 ? hideConfig.duration : 0.01f)
                    .SetEase(hideConfig.ease)
                    .SetUpdate(runWhilePaused)
                    .OnComplete(() => { if (disableAfterHide) gameObject.SetActive(false); });
            }
        }
    }
}
