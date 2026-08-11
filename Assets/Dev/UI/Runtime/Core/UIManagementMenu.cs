using System;
using UnityEngine;

namespace OZ.SentryIdle.UI.Core
{
    // 접이식 관리 메뉴의 표시 상태만 관리함
    public sealed class UIManagementMenu : MonoBehaviour
    {
        // 실제로 켜고 끌 메뉴 패널
        [SerializeField] private GameObject menuPanel;

        public event Action<bool> MenuOpenStateChanged;

        public bool IsOpen => menuPanel != null && menuPanel.activeSelf;

        private void Awake()
        {
            // 시작 화면에서는 메뉴를 숨김
            SetOpen(false, notify: false);
        }

        public void Toggle()
        {
            SetOpen(!IsOpen, notify: true);
        }

        public void Open()
        {
            SetOpen(true, notify: true);
        }

        public void Close()
        {
            SetOpen(false, notify: true);
        }

        private void SetOpen(bool open, bool notify)
        {
            // 상태가 바뀔 때만 알림을 보냄
            bool wasOpen = IsOpen;
            if (menuPanel != null)
            {
                menuPanel.SetActive(open);
            }

            if (notify && wasOpen != open)
            {
                MenuOpenStateChanged?.Invoke(open);
            }
        }

#if UNITY_EDITOR
        // Builder가 메뉴 패널을 넣을 때 사용함
        public void ConfigureForEditor(GameObject panel)
        {
            menuPanel = panel;
            SetOpen(false, notify: false);
        }
#endif
    }
}
