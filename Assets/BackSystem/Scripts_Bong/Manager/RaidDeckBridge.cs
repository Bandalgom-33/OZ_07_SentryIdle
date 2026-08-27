using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Raid.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

// 덱 매니저의 레이드 덱 편성을 레이드 런타임 시스템에 전달하는 연동 브릿지 컴포넌트
public sealed class RaidDeckBridge : MonoBehaviour
{
    private static RaidDeckBridge _instance;

    // 게임 시작 시 브릿지 인스턴스 자동 생성 및 씬 전환 유지 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeAutoBridge()
    {
        if (_instance != null)
        {
            return;
        }

        GameObject host = new GameObject("[Bridge] RaidDeckBridge");
        _instance = host.AddComponent<RaidDeckBridge>();
        DontDestroyOnLoad(host);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        SyncRaidDecksToRoster();
    }

    // 씬 로드 완료 시 레이드 로스터 동기화 처리
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncRaidDecksToRoster();
    }

    // 레이드 덱 변경 이벤트 수신 시 로스터 동기화 처리
    private void OnRaidDeckChanged(RaidDeckChangedEvent evt)
    {
        SyncRaidDecksToRoster();
    }

    // 세이브 데이터 로드 완료 이벤트 수신 시 로스터 동기화 처리
    private void OnDataLoaded(DataLoadEvent evt)
    {
        SyncRaidDecksToRoster();
    }

    // 레이드 1팀 및 2팀 덱 데이터를 조합하여 런타임 로스터로 전송 처리
    public static void SyncRaidDecksToRoster()
    {
        DeckManager deckManager = DeckManager.Instance;
        if (deckManager == null)
        {
            return;
        }

        const int slotsPerTeam = RaidRosterRuntime.SlotsPerTeam; // 8
        const int totalSlots = RaidRosterRuntime.TotalSlots;     // 16

        List<RaidRosterSelection> rosterSelections = new List<RaidRosterSelection>(totalSlots);

        // 1팀(Raid1) 8개 슬롯 데이터 수집
        List<DeckSlotUnitEntry> raid1Entries = deckManager.GetAllDeckSlotEntries(DeckType.Raid1);
        for (int i = 0; i < slotsPerTeam; i++)
        {
            UnitDataSO unitData = (i < raid1Entries.Count && raid1Entries[i].isOccupied) ? raid1Entries[i].unitData : null;
            rosterSelections.Add(unitData != null ? new RaidRosterSelection(unitData) : null);
        }

        // 2팀(Raid2) 8개 슬롯 데이터 수집
        List<DeckSlotUnitEntry> raid2Entries = deckManager.GetAllDeckSlotEntries(DeckType.Raid2);
        for (int i = 0; i < slotsPerTeam; i++)
        {
            UnitDataSO unitData = (i < raid2Entries.Count && raid2Entries[i].isOccupied) ? raid2Entries[i].unitData : null;
            rosterSelections.Add(unitData != null ? new RaidRosterSelection(unitData) : null);
        }

        // 1. 레이드 대기 덱 전송 서비스에 등록 시도 (16개 전체가 유효할 경우 성공)
        RaidRosterTransferService.SetPendingRoster(rosterSelections);

        // 2. 현재 활성화된 씬에 RaidRosterRuntime이 있다면 즉시 외부 로스터로 주입
        RaidRosterRuntime[] activeRosters = FindObjectsByType<RaidRosterRuntime>(FindObjectsSortMode.None);
        for (int i = 0; i < activeRosters.Length; i++)
        {
            RaidRosterRuntime runtime = activeRosters[i];
            if (runtime != null && runtime.isActiveAndEnabled)
            {
                runtime.SetExternalRoster(rosterSelections);
            }
        }
    }
}
