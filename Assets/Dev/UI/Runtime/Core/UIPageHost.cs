using System;
using System.Collections.Generic;
using UnityEngine;

namespace OZ.SentryIdle.UI.Core
{
    // Page 하나만 표시하고 이전 Page를 기억함
    public sealed class UIPageHost : MonoBehaviour
    {
        [Serializable]
        // Page ID와 오브젝트를 묶음
        public struct PageBinding
        {
            [SerializeField] private string pageId;
            [SerializeField] private GameObject pageRoot;

            public string PageId => pageId;
            public GameObject PageRoot => pageRoot;

            public PageBinding(string id, GameObject root)
            {
                pageId = id;
                pageRoot = root;
            }
        }

        // Page 뒤 입력을 막는 오브젝트
        [SerializeField] private GameObject pageInputBlocker;
        [SerializeField] private PageBinding[] pages;
        [SerializeField] private string initialPageId;

        // 이전 Page ID를 순서대로 저장함
        private readonly Stack<string> history = new();
        private string currentPageId;

        public event Action<bool> PageOpenStateChanged;

        public bool IsPageOpen => !string.IsNullOrEmpty(currentPageId);
        public string CurrentPageId => currentPageId;

        private void Awake()
        {
            // 시작 Page가 없으면 전부 숨김
            history.Clear();
            if (TryFindPage(initialPageId, out _))
            {
                ActivatePage(initialPageId);
            }
            else
            {
                DeactivateAllPages();
            }
        }

        public void OpenPage(string pageId)
        {
            // 현재 Page를 기록하고 새 Page를 표시함
            if (!TryFindPage(pageId, out _) || pageId == currentPageId)
            {
                return;
            }

            if (IsPageOpen)
            {
                history.Push(currentPageId);
            }

            ActivatePage(pageId);
        }

        public void GoBack()
        {
            // 기록된 이전 Page로 돌아감
            while (history.Count > 0)
            {
                string previousPageId = history.Pop();
                if (previousPageId != currentPageId && TryFindPage(previousPageId, out _))
                {
                    ActivatePage(previousPageId);
                    return;
                }
            }

            CloseCurrent();
        }

        public void CloseCurrent()
        {
            // 기록과 현재 Page를 전부 닫음
            history.Clear();
            DeactivateAllPages();
        }

        private void ActivatePage(string pageId)
        {
            bool wasOpen = IsPageOpen;
            for (int index = 0; index < GetPageCount(); index++)
            {
                PageBinding binding = pages[index];
                if (binding.PageRoot != null)
                {
                    binding.PageRoot.SetActive(binding.PageId == pageId);
                }
            }

            currentPageId = pageId;
            SetInputBlockerActive(true);
            if (!wasOpen)
            {
                PageOpenStateChanged?.Invoke(true);
            }
        }

        private void DeactivateAllPages()
        {
            bool wasOpen = IsPageOpen;
            for (int index = 0; index < GetPageCount(); index++)
            {
                if (pages[index].PageRoot != null)
                {
                    pages[index].PageRoot.SetActive(false);
                }
            }

            currentPageId = null;
            SetInputBlockerActive(false);
            if (wasOpen)
            {
                PageOpenStateChanged?.Invoke(false);
            }
        }

        private bool TryFindPage(string pageId, out GameObject pageRoot)
        {
            if (!string.IsNullOrWhiteSpace(pageId))
            {
                for (int index = 0; index < GetPageCount(); index++)
                {
                    PageBinding binding = pages[index];
                    if (binding.PageId == pageId && binding.PageRoot != null)
                    {
                        pageRoot = binding.PageRoot;
                        return true;
                    }
                }
            }

            pageRoot = null;
            return false;
        }

        private int GetPageCount()
        {
            return pages != null ? pages.Length : 0;
        }

        private void SetInputBlockerActive(bool active)
        {
            if (pageInputBlocker != null)
            {
                pageInputBlocker.SetActive(active);
            }
        }

#if UNITY_EDITOR
        // Builder가 Page 목록을 넣을 때 사용함
        public void ConfigureForEditor(
            GameObject blocker,
            PageBinding[] pageBindings,
            string initialId)
        {
            pageInputBlocker = blocker;
            pages = pageBindings;
            initialPageId = initialId;
            history.Clear();

            if (TryFindPage(initialPageId, out _))
            {
                ActivatePage(initialPageId);
            }
            else
            {
                DeactivateAllPages();
            }
        }

        public void SetInitialPageForEditor(string initialId)
        {
            // 시작 Page만 다시 지정함
            initialPageId = initialId;
            history.Clear();

            if (TryFindPage(initialPageId, out _))
            {
                ActivatePage(initialPageId);
            }
            else
            {
                DeactivateAllPages();
            }
        }
#endif
    }
}
