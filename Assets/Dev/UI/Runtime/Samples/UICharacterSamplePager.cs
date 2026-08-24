using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OZ.SentryIdle.UI.Samples
{
    // 캐릭터 카드 5개씩 페이지를 바꿈
    public sealed class UICharacterSamplePager : MonoBehaviour
    {
        // 페이지와 이동 UI를 연결함
        [SerializeField] private GameObject[] pages;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;

        // 현재 보이는 페이지 번호
        private int currentPageIndex;

        private void OnEnable()
        {
            // 다시 열릴 때 버튼과 표시를 맞춤
            BindButtons();
            ShowPage(currentPageIndex);
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void ShowPreviousPage()
        {
            ShowPage(currentPageIndex - 1);
        }

        private void ShowNextPage()
        {
            ShowPage(currentPageIndex + 1);
        }

        private void ShowPage(int pageIndex)
        {
            // 한 페이지만 표시함
            int pageCount = pages != null ? pages.Length : 0;
            if (pageCount == 0)
            {
                currentPageIndex = 0;
                if (pageLabel != null)
                {
                    pageLabel.text = "0 / 0";
                }

                SetButtonState(previousButton, false);
                SetButtonState(nextButton, false);
                return;
            }

            currentPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < pageCount; index++)
            {
                if (pages[index] != null)
                {
                    pages[index].SetActive(index == currentPageIndex);
                }
            }

            if (pageLabel != null)
            {
                pageLabel.text = $"{currentPageIndex + 1} / {pageCount}";
            }

            SetButtonState(previousButton, currentPageIndex > 0);
            SetButtonState(nextButton, currentPageIndex < pageCount - 1);
        }

        private void BindButtons()
        {
            // 중복 연결을 막고 다시 연결함
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

        private static void SetButtonState(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

#if UNITY_EDITOR
        // Builder가 참조를 넣을 때 사용함
        public void ConfigureForEditor(
            GameObject[] pageObjects,
            Button previous,
            Button next,
            TMP_Text label)
        {
            pages = pageObjects;
            previousButton = previous;
            nextButton = next;
            pageLabel = label;
            currentPageIndex = 0;
            ShowPage(currentPageIndex);
        }
#endif
    }
}
