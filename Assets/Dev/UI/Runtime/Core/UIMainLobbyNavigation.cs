using UnityEngine;
using UnityEngine.UI;

namespace OZ.SentryIdle.UI.Core
{
    // 메인 로비 버튼을 기존 가챠와 영웅 편성 화면에 연결함
    public sealed class UIMainLobbyNavigation : MonoBehaviour
    {
        [SerializeField] private Button shopSummonButton;
        [SerializeField] private Button heroFormationButton;
        [SerializeField] private string heroFormationPageId = "CharacterSample";

        private GachaUI gachaUI;
        private UIPageHost pageHost;

        private void OnEnable()
        {
            // 중복 연결을 막고 현재 버튼만 연결함
            if (shopSummonButton != null)
            {
                shopSummonButton.onClick.RemoveListener(OpenGacha);
                shopSummonButton.onClick.AddListener(OpenGacha);
            }

            if (heroFormationButton != null)
            {
                heroFormationButton.onClick.RemoveListener(OpenHeroFormation);
                heroFormationButton.onClick.AddListener(OpenHeroFormation);
            }
        }

        private void OnDisable()
        {
            // 비활성화될 때 런타임 연결을 정리함
            if (shopSummonButton != null)
            {
                shopSummonButton.onClick.RemoveListener(OpenGacha);
            }

            if (heroFormationButton != null)
            {
                heroFormationButton.onClick.RemoveListener(OpenHeroFormation);
            }
        }

        public void OpenGacha()
        {
            // 기존 가챠 패널을 표시함
            if (gachaUI == null)
            {
                gachaUI = FindFirstObjectByType<GachaUI>(FindObjectsInactive.Include);
            }

            if (gachaUI != null)
            {
                gachaUI.SetPanelActive(true);
            }
            else
            {
                Debug.LogWarning("[UIMainLobbyNavigation] GachaUI 참조가 없음", this);
            }
        }

        public void OpenHeroFormation()
        {
            // 기존 캐릭터 편성 Page를 표시함
            if (pageHost == null)
            {
                pageHost = FindFirstObjectByType<UIPageHost>(FindObjectsInactive.Include);
            }

            if (pageHost != null)
            {
                pageHost.OpenPage(heroFormationPageId);
            }
            else
            {
                Debug.LogWarning("[UIMainLobbyNavigation] UIPageHost 참조가 없음", this);
            }
        }
    }
}
