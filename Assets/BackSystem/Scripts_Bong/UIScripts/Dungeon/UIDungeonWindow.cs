using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 메인 패널 UI 윈도우 컨트롤러
public class UIDungeonWindow : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 헤더 정보 및 닫기 버튼 ---")]
    [Tooltip("메인 던전 창 타이틀 텍스트")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("던전 창 닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("--- 상단 파견 인원 현황 정보 ---")]
    [Tooltip("현재 파견 중인 총 유닛 수 텍스트")]
    [SerializeField] private TMP_Text currentlyAssignedCountText;

    [Tooltip("최대 파견 가능 총 슬롯 수 텍스트")]
    [SerializeField] private TMP_Text maxAssignableCountText;

    [Tooltip("미배치 빈 슬롯(배치 필요) 수 텍스트")]
    [SerializeField] private TMP_Text requiredAssignmentCountText;

    [Header("--- 3종 던전 카드 슬롯 뷰 ---")]
    [Tooltip("3개 던전 카드 UI 컴포넌트 배열")]
    [SerializeField] private UIDungeonCard[] dungeonCards = new UIDungeonCard[3];

    [Header("--- 팝업 모달 참조 ---")]
    [Tooltip("원스톱 유닛 파견 편성 모달")]
    [SerializeField] private UIDungeonFormationModal formationModal;

    #endregion

    #region 유니티 생명주기 및 초기화

    // 닫기 버튼 리스너 등록
    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    // 닫기 버튼 리스너 해제
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }
    }

    // 던전 카드 초기 데이터 바인딩
    private void Start()
    {
        InitializeDungeonCards();
    }

    // 창 활성화 시 카드 및 통계 갱신 및 이벤트 구독
    private void OnEnable()
    {
        EventBus.Subscribe<DungeonFormationChangedEvent>(OnFormationChanged);

        RefreshAllCards();
        RefreshDispatchStats();
    }

    // 창 비활성화 시 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DungeonFormationChangedEvent>(OnFormationChanged);
    }

    #endregion

    #region 카드 초기 바인딩 및 갱신

    // 3종 던전 카드 초기화 및 바인딩
    private void InitializeDungeonCards()
    {
        if (DungeonManager.Instance == null) return;

        IReadOnlyList<DungeonDataSO> dungeons = DungeonManager.Instance.GetAllDungeonData();
        if (dungeons == null || dungeons.Count == 0) return;

        for (int i = 0; i < dungeonCards.Length; i++)
        {
            if (dungeonCards[i] == null) continue;

            if (i < dungeons.Count && dungeons[i] != null)
            {
                string dId = dungeons[i].DungeonId;
                dungeonCards[i].gameObject.SetActive(true);
                dungeonCards[i].Bind(dId, OnOpenFormationModal);
            }
            else
            {
                dungeonCards[i].gameObject.SetActive(false);
            }
        }
    }

    // 활성화된 던전 카드 뷰 일괄 갱신
    public void RefreshAllCards()
    {
        for (int i = 0; i < dungeonCards.Length; i++)
        {
            if (dungeonCards[i] != null && dungeonCards[i].gameObject.activeSelf)
            {
                dungeonCards[i].RefreshCardView();
            }
        }
    }

    // 상단 3종 파견 현황 통계 계산 및 갱신
    public void RefreshDispatchStats()
    {
        if (DungeonManager.Instance == null) return;

        IReadOnlyList<DungeonDataSO> dungeons = DungeonManager.Instance.GetAllDungeonData();
        int dungeonCount = (dungeons != null) ? dungeons.Count : 3;

        int maxSlots = dungeonCount * DungeonManager.SlotsPerDungeon;
        int currentlyAssigned = 0;

        if (dungeons != null)
        {
            for (int i = 0; i < dungeons.Count; i++)
            {
                if (dungeons[i] == null) continue;

                int[] units = DungeonManager.Instance.GetAssignedUnitIds(dungeons[i].DungeonId);
                if (units != null)
                {
                    for (int s = 0; s < units.Length; s++)
                    {
                        if (units[s] > 0)
                        {
                            currentlyAssigned++;
                        }
                    }
                }
            }
        }

        int requiredAssignment = Mathf.Max(0, maxSlots - currentlyAssigned);

        if (currentlyAssignedCountText != null)
        {
            currentlyAssignedCountText.text = $"{currentlyAssigned}명";
        }

        if (maxAssignableCountText != null)
        {
            maxAssignableCountText.text = $"{maxSlots}명";
        }

        if (requiredAssignmentCountText != null)
        {
            requiredAssignmentCountText.text = $"{requiredAssignment}명";
        }
    }

    #endregion

    #region 이벤트 수신 핸들러

    // 편성 변경 이벤트 수신 시 카드 및 통계 갱신 처리
    private void OnFormationChanged(DungeonFormationChangedEvent evt)
    {
        if (gameObject.activeSelf)
        {
            RefreshAllCards();
            RefreshDispatchStats();
        }
    }

    #endregion

    #region 팀 편성 팝업 오픈 핸들러

    // 팀 편성 팝업 호출 처리
    private void OnOpenFormationModal(string dungeonId)
    {
        if (formationModal != null)
        {
            formationModal.OpenModal(dungeonId);
        }
    }

    #endregion

    #region 창 열기 / 닫기

    // 던전 메인 윈도우 활성화 처리
    public void OpenWindow()
    {
        gameObject.SetActive(true);
        RefreshAllCards();
        RefreshDispatchStats();
    }

    // 던전 메인 윈도우 비활성화 처리
    public void CloseWindow()
    {
        if (formationModal != null && formationModal.gameObject.activeSelf)
        {
            formationModal.CloseModal();
        }
        gameObject.SetActive(false);
    }

    #endregion
}
