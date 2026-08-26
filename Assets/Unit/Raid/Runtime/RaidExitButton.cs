using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class RaidExitButton : MonoBehaviour
    {
        [Header("로비 복귀")]
        [Tooltip("닫기 버튼을 눌렀을 때 이동할 로비 씬 이름입니다.")]
        [SerializeField] private string lobbySceneName = "TestBuild2MainLobby";

        private Button button;
        private bool isLoading;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(ExitToLobby);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ExitToLobby);
            }
        }

        public void ExitToLobby()
        {
            if (isLoading)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(lobbySceneName))
            {
                Debug.LogError("RaidExitButton의 Lobby Scene Name이 비어 있습니다.", this);
                return;
            }

            if (SceneManager.GetActiveScene().name == lobbySceneName)
            {
                return;
            }

            isLoading = true;
            button.interactable = false;

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(lobbySceneName, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                isLoading = false;
                button.interactable = true;
                Debug.LogError($"로비 씬을 불러오지 못했습니다. Scene: {lobbySceneName}", this);
            }
        }
    }
}
