using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OZ.SentryIdle.UI.Core
{
    // 하단 덱 패널의 표시 위치만 전환하는 UI 전용 컴포넌트
    public sealed class UIBottomDeckToggle : MonoBehaviour
    {
        [SerializeField] private RectTransform deckPanel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private RectTransform arrowIcon;
        [SerializeField, Min(0f)] private float collapsedOffset = 190f;
        [SerializeField, Min(0f)] private float animationDuration = 0.2f;

        private Coroutine moveRoutine;
        private float expandedPositionY;
        private bool isExpanded = true;

        private void Awake()
        {
            if (deckPanel == null)
            {
                enabled = false;
                return;
            }

            expandedPositionY = deckPanel.anchoredPosition.y;
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }

            UpdateArrow();
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }
        }

        public void Toggle()
        {
            SetExpanded(!isExpanded);
        }

        public void SetExpanded(bool expanded)
        {
            if (deckPanel == null || isExpanded == expanded)
            {
                return;
            }

            isExpanded = expanded;
            UpdateArrow();

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            // 패널 높이보다 작은 값이 들어와도 배경이 화면에 남지 않도록
            // 실제 높이를 최소 이동 거리로 사용함
            float collapsedDistance = Mathf.Max(collapsedOffset, deckPanel.rect.height);
            float targetY = isExpanded
                ? expandedPositionY
                : expandedPositionY - collapsedDistance;
            moveRoutine = StartCoroutine(MovePanel(targetY));
        }

        private IEnumerator MovePanel(float targetY)
        {
            Vector2 startPosition = deckPanel.anchoredPosition;
            Vector2 targetPosition = new(startPosition.x, targetY);

            if (animationDuration <= 0f)
            {
                deckPanel.anchoredPosition = targetPosition;
                moveRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / animationDuration);
                float easedProgress = progress * progress * (3f - 2f * progress);
                deckPanel.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    easedProgress);
                yield return null;
            }

            deckPanel.anchoredPosition = targetPosition;
            moveRoutine = null;
        }

        private void UpdateArrow()
        {
            if (arrowIcon != null)
            {
                arrowIcon.localRotation = Quaternion.Euler(0f, 0f, isExpanded ? -90f : 90f);
            }
        }
    }
}
