
using System;
using UnityEngine;

public enum CurrencyType
{
    Gold,
    Diamond,
    DpCost,
    WaveStone,
    StageStone,
    RaidStone
}

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

#region 덱 이벤트

// 덱 슬롯 편성 데이터 변경 이벤트
public readonly struct DeckChangedEvent
{
    // 변경된 10개 덱 슬롯 유닛 ID 배열 (크기 10, 미편성 시 -1)
    public readonly int[] deckSlots;

    // 덱 변경 이벤트 생성자
    public DeckChangedEvent(int[] deckSlots)
    {
        this.deckSlots = deckSlots != null ? (int[])deckSlots.Clone() : new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    }
}

#endregion