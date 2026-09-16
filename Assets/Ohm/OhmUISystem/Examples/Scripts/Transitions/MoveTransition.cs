using UnityEngine;
using DG.Tweening;

namespace Ohm.UISystem.Transitions
{
    public class MoveTransition : TransitionBase
    {
        public enum TargetMode { CurrentPosition, Transform, Vector3 }

        [System.Serializable]
        public struct MoveConfig
        {
            public TargetMode targetMode;
            [SerializeFieldIfEnum("targetMode", (int)TargetMode.Transform)]
            public Transform target;
            [SerializeFieldIfEnum("targetMode", (int)TargetMode.Vector3)]
            public Vector3 targetPosition;
            public float duration;
            public Ease ease;
        }

        [SerializeField] private MoveConfig showConfig = new(){ targetMode = TargetMode.CurrentPosition, duration = 0.5f, ease = Ease.OutQuad };
        [SerializeField] private MoveConfig hideConfig = new(){ targetMode = TargetMode.CurrentPosition, duration = 0.5f, ease = Ease.InQuad };

        private Tween moveToTween;

        private Vector3 GetTargetPos(MoveConfig config)
        {
            switch (config.targetMode)
            {
                case TargetMode.CurrentPosition:
                    return transform.localPosition;
                case TargetMode.Transform:
                    return config.target != null ? config.target.localPosition : transform.localPosition;
                case TargetMode.Vector3:
                    return config.targetPosition;
                default:
                    return transform.localPosition;
            }
        }

        public override float GetDuration(bool isShow) => isShow ? showConfig.duration : hideConfig.duration;

        public override bool SupportsCapture => true;

        public override void CaptureShowConfig()
        {
            showConfig.targetMode = TargetMode.Vector3;
            showConfig.targetPosition = transform.localPosition;
        }

        public override void CaptureHideConfig()
        {
            hideConfig.targetMode = TargetMode.Vector3;
            hideConfig.targetPosition = transform.localPosition;
        }

        public override void PrepareShow()
        {
            transform.localPosition = GetTargetPos(hideConfig);
        }

        protected override void PlayShow(bool instant)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            moveToTween?.Kill();

            if (instant)
            {
                transform.localPosition = GetTargetPos(showConfig);
            }
            else
            {
                transform.localPosition = GetTargetPos(hideConfig);
                moveToTween = transform.DOLocalMove(GetTargetPos(showConfig), showConfig.duration)
                    .SetEase(showConfig.ease)
                    .SetUpdate(runWhilePaused);
            }
        }

        protected override void PlayHide(bool instant)
        {
            moveToTween?.Kill();

            if (instant)
            {
                transform.localPosition = GetTargetPos(hideConfig);
                if (disableAfterHide) gameObject.SetActive(false);
            }
            else
            {
                moveToTween = transform.DOLocalMove(GetTargetPos(hideConfig), hideConfig.duration)
                    .SetEase(hideConfig.ease)
                    .SetUpdate(runWhilePaused)
                    .OnComplete(() => { if (disableAfterHide) gameObject.SetActive(false); });
            }
        }
    }
}
