using UnityEngine;
using DG.Tweening;

namespace Ohm.UISystem.Transitions
{
    public class SlideTransition : TransitionBase
    {
        public enum Direction { None, Left, Right, Top, Bottom }

        [System.Serializable]
        public struct SlideConfig
        {
            public Direction slideDirection;
            public bool customOffset;
            [SerializeFieldIf("customOffset")] public Vector2 offset;
            public float duration;
            public Ease ease;
            [HideInInspector] public Vector3 targetPosition;
        }

        [SerializeField] private SlideConfig showConfig = new(){ slideDirection = Direction.Left, offset = new Vector2(1920f, 1080f), duration = 0.5f, ease = Ease.OutCubic };
        [SerializeField] private SlideConfig hideConfig = new(){ slideDirection = Direction.Left, offset = new Vector2(1920f, 1080f), duration = 0.5f, ease = Ease.InCubic };

        [Space, Tooltip("Resting (shown) position. Captured at edit time via the Capture buttons.")]
        [SerializeField, ReadOnly] private Vector3 originalPos;

        private Tween slideTween;

        private Vector3 GetOffsetPos(SlideConfig config)
        {
            Vector3 pos = originalPos;
            Vector2 activeOffset = config.customOffset ? config.offset : GetRectSize();

            switch (config.slideDirection)
            {
                case Direction.None:   break;
                case Direction.Left:   pos.x -= activeOffset.x; break;
                case Direction.Right:  pos.x += activeOffset.x; break;
                case Direction.Top:    pos.y += activeOffset.y; break;
                case Direction.Bottom: pos.y -= activeOffset.y; break;
            }
            return pos;
        }

        private Vector2 GetRectSize()
        {
            RectTransform rect = transform as RectTransform;
            if (rect != null)
                return new Vector2(rect.rect.width, rect.rect.height);
            
            return new Vector2(1920f, 1080f); // Default fallback
        }

        public override float GetDuration(bool isShow) => isShow ? showConfig.duration : hideConfig.duration;

        public override bool SupportsCapture => true;

        // Slide derives both show and hide from a single resting pose, so both capture the same value.
        public override void CaptureShowConfig() => originalPos = transform.localPosition;

        public override void CaptureHideConfig() => originalPos = transform.localPosition;

        public override void PrepareShow()
        {
            transform.localPosition = GetOffsetPos(showConfig);
        }

        protected override void PlayShow(bool instant)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            slideTween?.Kill();

            if (instant)
            {
                transform.localPosition = originalPos;
            }
            else
            {
                transform.localPosition = GetOffsetPos(showConfig);
                slideTween = transform.DOLocalMove(originalPos, showConfig.duration)
                    .SetEase(showConfig.ease)
                    .SetUpdate(runWhilePaused);
            }
        }

        protected override void PlayHide(bool instant)
        {
            slideTween?.Kill();

            Vector3 hideEndPos = GetOffsetPos(hideConfig);

            if (instant)
            {
                transform.localPosition = hideEndPos;
                if (disableAfterHide) gameObject.SetActive(false);
            }
            else
            {
                slideTween = transform.DOLocalMove(hideEndPos, hideConfig.duration)
                    .SetEase(hideConfig.ease)
                    .SetUpdate(runWhilePaused)
                    .OnComplete(() => { if (disableAfterHide) gameObject.SetActive(false); });
            }
        }
    }
}
