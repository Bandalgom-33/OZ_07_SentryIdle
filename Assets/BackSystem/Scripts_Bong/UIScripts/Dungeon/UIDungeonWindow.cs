using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 던전 메인 윈도우 UI
public class UIDungeonWindow : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 3종 던전 카드 슬롯 뷰 ---")]
    [Tooltip("3개 던전 카드 UI 컴포넌트 배열")]
    [SerializeField] private UIDungeonCard[] dungeonCards = new UIDungeonCard[3];

    [Header("--- 팝업 모달 참조 ---")]
    [Tooltip("원스톱 유닛 파견 편성 모달")]
    [SerializeField] private UIDungeonFormationModal formationModal;

    [Header("--- 창 제어 버튼 ---")]
    [Tooltip("던전 창 닫기 버튼")]
    [SerializeField] private Button closeButton;

    #endregion

    #region 라이프사이클 및 초기화

    // 닫기 버튼 리스너 등록
    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    // 던전 카드 초기 바인딩
    private void Start()
    {
        InitializeDungeonCards();
    }

    // 던전 카드 전체 뷰 갱신
    private void OnEnable()
    {
        RefreshAllCards();
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

    #endregion

    #region 팀 편성 팝업 오픈 핸들러

    // 팀 편성 팝업 호출
    private void OnOpenFormationModal(string dungeonId)
    {
        if (formationModal != null)
        {
            formationModal.OpenModal(dungeonId);
        }
    }

    #endregion

    #region 창 열기 / 닫기

    // 던전 메인 윈도우 활성화
    public void OpenWindow()
    {
        gameObject.SetActive(true);
        RefreshAllCards();
    }

    // 던전 메인 윈도우 비활성화
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
