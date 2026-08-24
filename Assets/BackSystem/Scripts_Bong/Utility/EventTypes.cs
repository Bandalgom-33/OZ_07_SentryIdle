using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

public enum CurrencyType
{
    Gold,
    Diamond,
    DpCost,
    WaveStone,     // 일반 스테이지 및 웨이브 클리어 마석
    DungeonStone,  // 던전 파견 생산 마석 (기존 StageStone 개편)
    StageStone = DungeonStone, // 기존 코드 호환성용 별칭
    RaidStone      // 레이드 콘텐츠 마석
}

// 게임 내 주요 씬 종류 열거형
public enum SceneType
{
    Lobby = 0,     // 메인 로비 씬 (패널 및 관리 화면)
    GamePlay = 1,  // 방치형 전투 진행 씬
    Raid = 2       // 레이드 보스전 씬
}

#region 씬 전환 이벤트

public readonly struct SceneLoadStartEvent
{
    public readonly SceneType targetScene;
    public readonly string sceneName;

    // 씬 로드 시작 이벤트 생성자
    public SceneLoadStartEvent(SceneType targetScene, string sceneName)
    {
        this.targetScene = targetScene;
        this.sceneName = sceneName ?? string.Empty;
    }
}

public readonly struct SceneLoadProgressEvent
{
    public readonly SceneType targetScene;
    public readonly float progressRatio;

    // 씬 로드 진행률 이벤트 생성자
    public SceneLoadProgressEvent(SceneType targetScene, float progressRatio)
    {
        this.targetScene = targetScene;
        this.progressRatio = progressRatio;
    }
}

public readonly struct SceneLoadCompletedEvent
{
    public readonly SceneType loadedScene;
    public readonly string sceneName;

    // 씬 로드 완료 이벤트 생성자
    public SceneLoadCompletedEvent(SceneType loadedScene, string sceneName)
    {
        this.loadedScene = loadedScene;
        this.sceneName = sceneName ?? string.Empty;
    }
}

#endregion

#region 게임 상태 및 세팅 이벤트
    
    public readonly struct GameStateChangedEvent
    {
        public readonly GameState previousState;
        public readonly GameState newState;

        // 게임 상태 변경 이벤트 생성자
        public GameStateChangedEvent(GameState previousState, GameState newState)
        {
            this.previousState = previousState;
            this.newState = newState;
        }
    }

    public readonly struct GameSpeedChangedEvent
    {
        public readonly int speedIndex;
        public readonly float timeScale;

        // 게임 속도 변경 이벤트 생성자
        public GameSpeedChangedEvent(int speedIndex, float timeScale)
        {
            this.speedIndex = speedIndex;
            this.timeScale = timeScale;
        }
    }

#endregion

#region 재화 및 스탯 이벤트
    
    public readonly struct CurrencyChangedEvent
    {
        public readonly CurrencyType currencyType; 
        public readonly long currentAmount;
        public readonly long changeAmount; 

        // 재화 변경 이벤트 생성자
        public CurrencyChangedEvent(CurrencyType currencyType, long currentAmount, long changeAmount)
        {
            this.currencyType = currencyType;
            this.currentAmount = currentAmount;
            this.changeAmount = changeAmount;
        }
    }

    public readonly struct StatUpgradedEvent
    {
        public readonly string statType; 
        public readonly int newLevel;    

        // 스탯 업그레이드 이벤트 생성자
        public StatUpgradedEvent(string statType, int newLevel)
        {
            this.statType = statType;
            this.newLevel = newLevel;
        }
    }

#endregion

#region 스테이지 및 디펜스 전투 이벤트

    // 스테이지 및 웨이브 변경 알림 이벤트
    public readonly struct StageWaveChangedEvent
    {
        public readonly int stageNumber;
        public readonly int waveNumber;

        // 스테이지 및 웨이브 변경 이벤트 생성자
        public StageWaveChangedEvent(int stageNumber, int waveNumber)
        {
            this.stageNumber = stageNumber;
            this.waveNumber = waveNumber;
        }
    }

    // 웨이브 클리어 시 발행되는 이벤트 (웨이브 마석 보상 포함)
    public readonly struct WaveClearedEvent
    {
        public readonly int stageNumber;
        public readonly int waveNumber;
        public readonly int rewardWaveStone;

        // 웨이브 클리어 이벤트 생성자
        public WaveClearedEvent(int stageNumber, int waveNumber, int rewardWaveStone)
        {
            this.stageNumber = stageNumber;
            this.waveNumber = waveNumber;
            this.rewardWaveStone = rewardWaveStone;
        }
    }

    // 스테이지(전체 웨이브 완료) 클리어 시 발행되는 이벤트 (웨이브 마석 보상 포함)
    public readonly struct StageClearedEvent
    {
        public readonly int stageNumber;
        public readonly int rewardWaveStone;

        // 스테이지 클리어 이벤트 생성자
        public StageClearedEvent(int stageNumber, int rewardWaveStone)
        {
            this.stageNumber = stageNumber;
            this.rewardWaveStone = rewardWaveStone;
        }
    }

    // 아군 유닛의 필드 소환/사망(퇴각) 상태 변경 알림 이벤트 (HUD 덱 슬롯 명암 처리용)
    public readonly struct UnitFieldSpawnStateChangedEvent
    {
        public readonly string unitKey;   // 예: "UNIT_0001"
        public readonly bool isSpawned;   // true: 필드 배치 중, false: 사망 또는 미배치

        // 유닛 필드 소환 상태 변경 이벤트 생성자
        public UnitFieldSpawnStateChangedEvent(string unitKey, bool isSpawned)
        {
            this.unitKey = unitKey ?? string.Empty;
            this.isSpawned = isSpawned;
        }
    }

    public readonly struct EnemyDiedEvent
    {
        public readonly UnityEngine.GameObject enemyGameObject;
        public readonly string enemyId;
        public readonly int rewardGold;
        public readonly int rewardExp;
        public readonly UnityEngine.Vector3 position;

        // 적 사망 이벤트 생성자
        public EnemyDiedEvent(UnityEngine.GameObject enemyGameObject, string enemyId, int rewardGold, int rewardExp, UnityEngine.Vector3 position)
        {
            this.enemyGameObject = enemyGameObject;
            this.enemyId = enemyId ?? string.Empty;
            this.rewardGold = rewardGold;
            this.rewardExp = rewardExp;
            this.position = position;
        }
    }

#endregion

#region 저장 이벤트

public readonly struct DataSaveEvent
{
    public readonly SaveData saveData;

    // 데이터 저장 이벤트 생성자
    public DataSaveEvent(SaveData saveData)
    {
        this.saveData = saveData;
    }
}

public readonly struct DataLoadEvent
{
    public readonly SaveData saveData;

    // 데이터 로드 이벤트 생성자
    public DataLoadEvent(SaveData saveData)
    {
        this.saveData = saveData;
    }
}

public readonly struct DataResetEvent
{
    
}

#endregion

#region 가챠 이벤트

public readonly struct GachaDrawCompletedEvent
{
    public readonly System.Collections.Generic.List<IGachaRewardItem> resultItems;
    public readonly int currentPityStack;

    // 가챠 완료 이벤트 생성자
    public GachaDrawCompletedEvent(System.Collections.Generic.List<IGachaRewardItem> resultItems, int currentPityStack)
    {
        this.resultItems = resultItems;
        this.currentPityStack = currentPityStack;
    }
}

#endregion


// 덱 종류를 명확하게 구분하기 위한 열거형 (일반 필드 1개, 레이드 2개 지원)
public enum DeckType
{
    Normal = 0, // 일반 스테이지 디펜스/필드 전투용 덱
    Raid1 = 1,  // 레이드 1팀 덱
    Raid2 = 2   // 레이드 2팀 덱
}

#region 덱 슬롯 및 이벤트 데이터 구조체

// 개별 덱 슬롯에 편성된 유닛의 풀패키지 정보 (슬롯 인덱스, 정수/문자열 ID, ScriptableObject 데이터)
[Serializable]
public readonly struct DeckSlotUnitEntry
{
    // 슬롯 번호 (0-based)
    public readonly int slotIndex;
    // 유닛 고유 정수 ID (미배치 시 -1)
    public readonly int unitId;
    // 유닛 식별 문자열 키 (예: "UNIT_0001", 미배치 시 빈 문자열)
    public readonly string unitKey;
    // 유닛 스탯/메타데이터 SO (미배치 시 null)
    public readonly UnitDataSO unitData;
    // 슬롯에 유효한 유닛이 장착되어 있는지 여부
    public readonly bool isOccupied;

    // 슬롯 정보 생성자
    public DeckSlotUnitEntry(int slotIndex, int unitId, string unitKey, UnitDataSO unitData)
    {
        this.slotIndex = slotIndex;
        this.unitId = unitId;
        this.unitKey = unitKey ?? string.Empty;
        this.unitData = unitData;
        this.isOccupied = unitId > 0 && unitData != null;
    }
}

// 일반 필드 덱 편성 변경 이벤트 (인덱스, 유닛 ID, UnitDataSO를 포함하여 독립 발행)
public readonly struct NormalDeckChangedEvent
{
    // 전체 슬롯 목록 (미배치 빈 슬롯 포함)
    public readonly IReadOnlyList<DeckSlotUnitEntry> allSlots;
    // 실제 유닛이 배치된 유효 슬롯 목록 (빈 슬롯 제외)
    public readonly IReadOnlyList<DeckSlotUnitEntry> activeUnits;
    // 실제 등록된 유닛 문자열 키 리스트 (예: ["UNIT_0001", "UNIT_0002"])
    public readonly IReadOnlyList<string> registeredUnitKeys;
    // 실제 등록된 유닛 정수 ID 리스트 (예: [1, 2])
    public readonly IReadOnlyList<int> registeredUnitIds;
    // 실제 등록된 유닛 UnitDataSO 리스트 (스폰/전투 로직에서 즉시 활용)
    public readonly IReadOnlyList<UnitDataSO> registeredUnitDatas;

    // 일반 덱 변경 이벤트 생성자
    public NormalDeckChangedEvent(
        List<DeckSlotUnitEntry> allSlots,
        List<DeckSlotUnitEntry> activeUnits,
        List<string> registeredUnitKeys,
        List<int> registeredUnitIds,
        List<UnitDataSO> registeredUnitDatas)
    {
        this.allSlots = allSlots ?? new List<DeckSlotUnitEntry>();
        this.activeUnits = activeUnits ?? new List<DeckSlotUnitEntry>();
        this.registeredUnitKeys = registeredUnitKeys ?? new List<string>();
        this.registeredUnitIds = registeredUnitIds ?? new List<int>();
        this.registeredUnitDatas = registeredUnitDatas ?? new List<UnitDataSO>();
    }
}

// 레이드 전용 덱 편성 변경 이벤트 (레이드 1팀/2팀 구분, 인덱스, 유닛 ID, UnitDataSO를 포함하여 독립 발행)
public readonly struct RaidDeckChangedEvent
{
    // 변경된 레이드 팀 구분 (DeckType.Raid1 또는 DeckType.Raid2)
    public readonly DeckType raidTeamType;
    // 전체 슬롯 목록 (미배치 빈 슬롯 포함)
    public readonly IReadOnlyList<DeckSlotUnitEntry> allSlots;
    // 실제 유닛이 배치된 유효 슬롯 목록 (빈 슬롯 제외)
    public readonly IReadOnlyList<DeckSlotUnitEntry> activeUnits;
    // 실제 등록된 유닛 문자열 키 리스트
    public readonly IReadOnlyList<string> registeredUnitKeys;
    // 실제 등록된 유닛 정수 ID 리스트
    public readonly IReadOnlyList<int> registeredUnitIds;
    // 실제 등록된 유닛 UnitDataSO 리스트 (레이드 보스전 스폰 시 즉시 사용)
    public readonly IReadOnlyList<UnitDataSO> registeredUnitDatas;

    // 레이드 덱 변경 이벤트 생성자
    public RaidDeckChangedEvent(
        DeckType raidTeamType,
        List<DeckSlotUnitEntry> allSlots,
        List<DeckSlotUnitEntry> activeUnits,
        List<string> registeredUnitKeys,
        List<int> registeredUnitIds,
        List<UnitDataSO> registeredUnitDatas)
    {
        this.raidTeamType = raidTeamType;
        this.allSlots = allSlots ?? new List<DeckSlotUnitEntry>();
        this.activeUnits = activeUnits ?? new List<DeckSlotUnitEntry>();
        this.registeredUnitKeys = registeredUnitKeys ?? new List<string>();
        this.registeredUnitIds = registeredUnitIds ?? new List<int>();
        this.registeredUnitDatas = registeredUnitDatas ?? new List<UnitDataSO>();
    }
}

// 기존 단일 덱 하위 호환용 이벤트 (이전 코드와의 호환성을 유지하기 위해 보존)
public readonly struct DeckChangedEvent
{
    // 변경된 덱 종류
    public readonly DeckType deckType;
    // 덱 슬롯 유닛 ID 배열 (미편성 시 -1)
    public readonly int[] deckSlots;

    // 기존 단일 덱 호환 생성자
    public DeckChangedEvent(int[] deckSlots)
    {
        this.deckType = DeckType.Normal;
        this.deckSlots = deckSlots != null ? (int[])deckSlots.Clone() : new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    }

    // 덱 타입 포함 생성자
    public DeckChangedEvent(DeckType deckType, int[] deckSlots)
    {
        this.deckType = deckType;
        this.deckSlots = deckSlots != null ? (int[])deckSlots.Clone() : Array.Empty<int>();
    }
}

// UI 등에서 DeckManager를 직접 참조하지 않고 특정 덱 슬롯 장착을 요청하는 커맨드 이벤트
public readonly struct RequestSetDeckSlotEvent
{
    public readonly DeckType deckType;
    public readonly int slotIndex;
    public readonly string unitKey; // 빈 문자열 또는 null일 경우 해당 슬롯 해제

    // 슬롯 장착 요청 생성자
    public RequestSetDeckSlotEvent(DeckType deckType, int slotIndex, string unitKey)
    {
        this.deckType = deckType;
        this.slotIndex = slotIndex;
        this.unitKey = unitKey;
    }
}

// UI 등에서 빈 슬롯에 자동 유닛 배치를 요청하는 커맨드 이벤트
public readonly struct RequestAutoAddDeckEvent
{
    public readonly DeckType deckType;
    public readonly string unitKey;

    // 자동 배치 요청 생성자
    public RequestAutoAddDeckEvent(DeckType deckType, string unitKey)
    {
        this.deckType = deckType;
        this.unitKey = unitKey;
    }
}

#endregion

#region 던전 방치형 생산 및 편성 이벤트

// 던전 실시간 생산 진행도(슬라이더) 갱신 이벤트
public readonly struct DungeonProgressUpdatedEvent
{
    public readonly string dungeonId;          // 대상 던전 고유 ID (예: DUNGEON_01)
    public readonly float progressRatio;       // 현재 진행도 (0.0f ~ 1.0f)
    public readonly float remainingSeconds;    // 1회 완료까지 남은 시간(초)
    public readonly bool isRunning;            // 현재 전투력 충족으로 가동 중인지 여부

    // 던전 진행도 변경 이벤트 생성자
    public DungeonProgressUpdatedEvent(string dungeonId, float progressRatio, float remainingSeconds, bool isRunning)
    {
        this.dungeonId = dungeonId;
        this.progressRatio = progressRatio;
        this.remainingSeconds = remainingSeconds;
        this.isRunning = isRunning;
    }
}

// 던전 1회 생산 주기 완료 및 보상 즉시 지급 이벤트
public readonly struct DungeonCycleCompletedEvent
{
    public readonly string dungeonId;          // 완료된 던전 ID
    public readonly long rewardedGold;         // 지급된 최종 골드 수량
    public readonly long rewardedDiamond;      // 지급된 최종 다이아 수량
    public readonly long rewardedStone;        // 지급된 최종 스테이지 마석 수량
    public readonly float bonusRatio;          // 초과 전투력으로 적용된 보너스 배율

    // 던전 생산 완료 이벤트 생성자
    public DungeonCycleCompletedEvent(string dungeonId, long rewardedGold, long rewardedDiamond, long rewardedStone, float bonusRatio)
    {
        this.dungeonId = dungeonId;
        this.rewardedGold = rewardedGold;
        this.rewardedDiamond = rewardedDiamond;
        this.rewardedStone = rewardedStone;
        this.bonusRatio = bonusRatio;
    }
}

// 던전 유닛 파견 편성 변경 시 UI 및 상태 동기화 이벤트
public readonly struct DungeonFormationChangedEvent
{
    public readonly string dungeonId;          // 변경된 던전 ID
    public readonly int[] assignedUnitIds;     // 배치된 3개 유닛 ID 배열 (-1: 미배치)
    public readonly int totalCombatPower;      // 계산된 현재 던전 총 전투력
    public readonly int requiredMinPower;      // 해당 던전 최소 요구 전투력
    public readonly float bonusRatio;          // 계산된 초과 보너스 배율
    public readonly bool isRunning;            // 생산 가동 여부 (총 전투력 >= 요구 전투력)

    // 던전 편성 변경 이벤트 생성자
    public DungeonFormationChangedEvent(
        string dungeonId,
        int[] assignedUnitIds,
        int totalCombatPower,
        int requiredMinPower,
        float bonusRatio,
        bool isRunning)
    {
        this.dungeonId = dungeonId;
        this.assignedUnitIds = assignedUnitIds != null ? (int[])assignedUnitIds.Clone() : new int[3] { -1, -1, -1 };
        this.totalCombatPower = totalCombatPower;
        this.requiredMinPower = requiredMinPower;
        this.bonusRatio = bonusRatio;
        this.isRunning = isRunning;
    }
}

#endregion