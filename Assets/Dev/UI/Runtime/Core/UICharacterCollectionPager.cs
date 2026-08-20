using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OZ.SentryIdle.UI.Core
{
    // 연결 담당자가 필터 적용 후 실제로 표시할 캐릭터 수를 전달하는 UI 이벤트
    public readonly struct CharacterCollectionViewChangedEvent
    {
        public CharacterCollectionViewChangedEvent(int visibleCardCount)
        {
            VisibleCardCount = Mathf.Max(0, visibleCardCount);
        }

        public int VisibleCardCount { get; }
    }

    // 보유 캐릭터 수에 맞춰 카드 페이지와 이동 UI의 표시 상태만 관리함
    public sealed class UICharacterCollectionPager : MonoBehaviour
    {
        [SerializeField] private GameObject[] pages;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField, Min(1)] private int itemsPerPage = 5;
        [SerializeField, Min(0)] private int initialVisibleCardCount = 5;

        private int currentPageIndex;
        private int visibleCardCount;
        private bool initialized;

        private void OnEnable()
        {
            if (!initialized)
            {
                visibleCardCount = Mathf.Max(0, initialVisibleCardCount);
                initialized = true;
            }

            BindButtons();
            EventBus.Subscribe<CharacterCollectionViewChangedEvent>(HandleCollectionViewChanged);
            RefreshDisplay();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CharacterCollectionViewChangedEvent>(HandleCollectionViewChanged);
            UnbindButtons();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            itemsPerPage = Mathf.Max(1, itemsPerPage);
            initialVisibleCardCount = Mathf.Max(0, initialVisibleCardCount);

            // 플레이 중 Inspector 테스트 값도 즉시 페이지 UI에 반영함
            if (Application.isPlaying && isActiveAndEnabled)
            {
                visibleCardCount = initialVisibleCardCount;
                initialized = true;
                RefreshDisplay();
            }
        }
#endif

        // 연결 담당자가 이벤트 대신 직접 최신 표시 카드 수를 전달할 때도 사용할 수 있음
        public void RefreshPages(int newVisibleCardCount)
        {
            visibleCardCount = Mathf.Max(0, newVisibleCardCount);
            RefreshDisplay();
        }

        // 동적으로 페이지 컨테이너를 만든 뒤 연결 담당자가 최신 목록을 다시 넣을 때 사용함
        public void SetPageRoots(GameObject[] pageRoots)
        {
            pages = pageRoots;
            RefreshDisplay();
        }

        private void HandleCollectionViewChanged(CharacterCollectionViewChangedEvent evt)
        {
            RefreshPages(evt.VisibleCardCount);
        }

        private void ShowPreviousPage()
        {
            ShowPage(currentPageIndex - 1);
        }

        private void ShowNextPage()
        {
            ShowPage(currentPageIndex + 1);
        }

        private void RefreshDisplay()
        {
            int pageCount = GetAvailablePageCount();
            if (pageCount == 0)
            {
                currentPageIndex = 0;
                DeactivateAllPages();
                SetPagerControlsActive(false);
                return;
            }

            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, pageCount - 1);
            ShowPage(currentPageIndex);
        }

        private void ShowPage(int pageIndex)
        {
            int pageCount = GetAvailablePageCount();
            if (pageCount == 0)
            {
                RefreshDisplay();
                return;
            }

            currentPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < GetPageRootCount(); index++)
            {
                if (pages[index] != null)
                {
                    pages[index].SetActive(index < pageCount && index == currentPageIndex);
                }
            }

            bool hasMultiplePages = pageCount > 1;
            SetObjectActive(pageLabel != null ? pageLabel.gameObject : null, hasMultiplePages);
            SetObjectActive(previousButton != null ? previousButton.gameObject : null, hasMultiplePages);
            SetObjectActive(nextButton != null ? nextButton.gameObject : null, hasMultiplePages);

            // 2페이지 이상이면 양쪽 화살표를 항상 표시하고 이동 불가능한 방향만 비활성화함
            SetButtonInteractable(previousButton, hasMultiplePages && currentPageIndex > 0);
            SetButtonInteractable(nextButton, hasMultiplePages && currentPageIndex < pageCount - 1);

            if (pageLabel != null && hasMultiplePages)
            {
                pageLabel.text = $"{currentPageIndex + 1} / {pageCount}";
            }
        }

        private int GetAvailablePageCount()
        {
            if (visibleCardCount <= 0 || itemsPerPage <= 0)
            {
                return 0;
            }

            int requiredPageCount = Mathf.CeilToInt(visibleCardCount / (float)itemsPerPage);
            return Mathf.Min(requiredPageCount, GetPageRootCount());
        }

        private int GetPageRootCount()
        {
            return pages != null ? pages.Length : 0;
        }

        private void DeactivateAllPages()
        {
            for (int index = 0; index < GetPageRootCount(); index++)
            {
                if (pages[index] != null)
                {
                    pages[index].SetActive(false);
                }
            }
        }

        private void SetPagerControlsActive(bool active)
        {
            SetObjectActive(pageLabel != null ? pageLabel.gameObject : null, active);
            SetObjectActive(previousButton != null ? previousButton.gameObject : null, active);
            SetObjectActive(nextButton != null ? nextButton.gameObject : null, active);
        }

        private void BindButtons()
        {
            UnbindButtons();
            if (previousButton != null)
            {
                previousButton.onClick.AddListener(ShowPreviousPage);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(ShowNextPage);
            }
        }

        private void UnbindButtons()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(ShowPreviousPage);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(ShowNextPage);
            }
        }

        private static void SetObjectActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
