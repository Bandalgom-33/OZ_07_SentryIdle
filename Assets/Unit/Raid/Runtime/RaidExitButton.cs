using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class RaidExitButton : MonoBehaviour
    {
        private Button button;

        // 컴포넌트 참조 초기화 연산
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        // 버튼 클릭 이벤트 리스너 등록
        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(ExitToLobby);
        }

        // 버튼 클릭 이벤트 리스너 해제
        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ExitToLobby);
            }
        }

        // 로비 씬 전환 요청 처리
        public void ExitToLobby()
        {
            if (SceneLoader.Instance == null || SceneLoader.Instance.IsLoading)
            {
                return;
            }

            SceneLoader.Instance.LoadScene(SceneType.Lobby);
        }
    }
}
