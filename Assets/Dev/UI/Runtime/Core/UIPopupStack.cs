using System;
using System.Collections.Generic;
using UnityEngine;

namespace OZ.SentryIdle.UI.Core
{
    // 열린 Popup 순서와 Dimmer를 관리함
    public sealed class UIPopupStack : MonoBehaviour
    {
        [Serializable]
        // Popup ID와 오브젝트를 묶음
        public struct PopupBinding
        {
            [SerializeField] private string popupId;
            [SerializeField] private GameObject popupRoot;

            public string PopupId => popupId;
            public GameObject PopupRoot => popupRoot;

            public PopupBinding(string id, GameObject root)
            {
                popupId = id;
                popupRoot = root;
            }
        }

        // Popup 뒤 입력을 막는 Dimmer
        [SerializeField] private GameObject popupDimmer;
        [SerializeField] private PopupBinding[] popups;

        // 열린 순서대로 ID를 저장함
        private readonly List<string> openPopupIds = new();

        public event Action<bool> PopupOpenStateChanged;

        public bool HasOpenPopup => openPopupIds.Count > 0;
        public int OpenPopupCount => openPopupIds.Count;
        public string TopPopupId => HasOpenPopup ? openPopupIds[^1] : null;

        private void Awake()
        {
            // 시작할 때 Popup을 전부 숨김
            DeactivateAllPopups();
        }

        public void OpenPopup(string popupId)
        {
            // 같은 Popup은 중복으로 열지 않음
            if (!TryFindPopup(popupId, out GameObject popupRoot) || openPopupIds.Contains(popupId))
            {
                return;
            }

            bool wasOpen = HasOpenPopup;
            openPopupIds.Add(popupId);
            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
            SetDimmerActive(true);

            if (!wasOpen)
            {
                PopupOpenStateChanged?.Invoke(true);
            }
        }

        public void CloseTop()
        {
            // 마지막에 연 Popup만 닫음
            if (!HasOpenPopup)
            {
                return;
            }

            int topIndex = openPopupIds.Count - 1;
            string popupId = openPopupIds[topIndex];
            openPopupIds.RemoveAt(topIndex);

            if (TryFindPopup(popupId, out GameObject popupRoot))
            {
                popupRoot.SetActive(false);
            }

            RefreshOpenState();
        }

        public void CloseAll()
        {
            // Popup과 Dimmer를 전부 닫음
            if (!HasOpenPopup)
            {
                SetDimmerActive(false);
                return;
            }

            DeactivateAllPopups();
            PopupOpenStateChanged?.Invoke(false);
        }

        private void DeactivateAllPopups()
        {
            for (int index = 0; index < GetPopupCount(); index++)
            {
                GameObject popupRoot = popups[index].PopupRoot;
                if (popupRoot != null)
                {
                    popupRoot.SetActive(false);
                }
            }

            openPopupIds.Clear();
            SetDimmerActive(false);
        }

        private void RefreshOpenState()
        {
            bool hasOpenPopup = HasOpenPopup;
            SetDimmerActive(hasOpenPopup);
            if (!hasOpenPopup)
            {
                PopupOpenStateChanged?.Invoke(false);
            }
        }

        private bool TryFindPopup(string popupId, out GameObject popupRoot)
        {
            if (!string.IsNullOrWhiteSpace(popupId))
            {
                for (int index = 0; index < GetPopupCount(); index++)
                {
                    PopupBinding binding = popups[index];
                    if (binding.PopupId == popupId && binding.PopupRoot != null)
                    {
                        popupRoot = binding.PopupRoot;
                        return true;
                    }
                }
            }

            popupRoot = null;
            return false;
        }

        private int GetPopupCount()
        {
            return popups != null ? popups.Length : 0;
        }

        private void SetDimmerActive(bool active)
        {
            if (popupDimmer != null)
            {
                popupDimmer.SetActive(active);
            }
        }

#if UNITY_EDITOR
        // Builder가 Popup 목록을 넣을 때 사용함
        public void ConfigureForEditor(GameObject dimmer, PopupBinding[] popupBindings)
        {
            popupDimmer = dimmer;
            popups = popupBindings;
            DeactivateAllPopups();
        }
#endif
    }
}
