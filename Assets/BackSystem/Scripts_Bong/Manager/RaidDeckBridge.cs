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

    // 이벤트 버스 및 씬 로드 이벤트 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoaded);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // 이벤트 버스 및 씬 로드 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoaded);
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // 초기 레이드 로스터 동기화 시작
    private void Start()
    {
        SyncRaidDecksToRoster();
    }

    // 씬 로드 완료 이벤트 콜백
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncRaidDecksToRoster();
    }

    // 레이드 덱 변경 이벤트 콜백
    private void OnRaidDeckChanged(RaidDeckChangedEvent evt)
    {
        SyncRaidDecksToRoster();
    }

    // 세이브 데이터 로드 이벤트 콜백
    private void OnDataLoaded(DataLoadEvent evt)
    {
        SyncRaidDecksToRoster();
    }

    // 레이드 덱 데이터를 조합하여 런타임 로스터로 전송
    public static void SyncRaidDecksToRoster()
    {
        DeckManager deckManager = DeckManager.Instance;
        if (deckManager == null)
        {
            return;
        }

        const int slotsPerTeam = RaidRosterRuntime.SlotsPerTeam;
        const int totalSlots = RaidRosterRuntime.TotalSlots;

        List<RaidRosterSelection> rosterSelections = new List<RaidRosterSelection>(totalSlots);

        List<DeckSlotUnitEntry> raid1Entries = deckManager.GetAllDeckSlotEntries(DeckType.Raid1);
        for (int i = 0; i < slotsPerTeam; i++)
        {
            UnitDataSO unitData = (i < raid1Entries.Count && raid1Entries[i].isOccupied) ? raid1Entries[i].unitData : null;
            rosterSelections.Add(unitData != null ? new RaidRosterSelection(unitData) : null);
        }

        List<DeckSlotUnitEntry> raid2Entries = deckManager.GetAllDeckSlotEntries(DeckType.Raid2);
        for (int i = 0; i < slotsPerTeam; i++)
        {
            UnitDataSO unitData = (i < raid2Entries.Count && raid2Entries[i].isOccupied) ? raid2Entries[i].unitData : null;
            rosterSelections.Add(unitData != null ? new RaidRosterSelection(unitData) : null);
        }

        RaidRosterTransferService.SetPendingRoster(rosterSelections);

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
