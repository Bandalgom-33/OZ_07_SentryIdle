
using System;
using UnityEngine;

// 게임 내 6대 재화 종류 정의
public enum CurrencyType
{
    Gold,
    Diamond,
    DpCost,
    WaveStone,   // 보스 라운드(5라운드) 클리어 마석
    StageStone,  // 원정 방치 던전 클리어 마석
    RaidStone    // 보스 레이드 클리어 마석
}

#region 게임 상태 및 세팅 이벤트
    
    //게임 상태 변경
    public readonly struct GameStateChangedEvent
    {
        public readonly GameState previousState;
        public readonly GameState newState;

        public GameStateChangedEvent(GameState previousState, GameState newState)
        {
            this.previousState = previousState;
            this.newState = newState;
        }
    }

    //게임 속도 변경
    public readonly struct GameSpeedChangedEvent
    {
        public readonly int speedIndex;
        public readonly float timeScale;

        public GameSpeedChangedEvent(int speedIndex, float timeScale)
        {
            this.speedIndex = speedIndex;
            this.timeScale = timeScale;
        }
    }

#endregion

#region 재화 및 스탯 이벤트
    
    // 재화 변경 이벤트
    public readonly struct CurrencyChangedEvent
    {
        public readonly CurrencyType currencyType; 
        public readonly long currentAmount;
        public readonly long changeAmount; 

        public CurrencyChangedEvent(CurrencyType currencyType, long currentAmount, long changeAmount)
        {
            this.currencyType = currencyType;
            this.currentAmount = currentAmount;
            this.changeAmount = changeAmount;
        }
    }

    // 스텟 변경 이벤트
    public readonly struct StatUpgradedEvent
    {
        public readonly string statType; 
        public readonly int newLevel;    

        public StatUpgradedEvent(string statType, int newLevel)
        {
            this.statType = statType;
            this.newLevel = newLevel;
        }
    }

#endregion

#region 스테이지 및 디펜스 전투 이벤트

    // 스테이지/ 웨이브 변경 이벤트
    public readonly struct StageWaveChangedEvent
    {
        public readonly int stageNumber;
        public readonly int waveNumber;

        public StageWaveChangedEvent(int stageNumber, int waveNumber)
        {
            this.stageNumber = stageNumber;
            this.waveNumber = waveNumber;
        }
    }

    // 적 사망 이벤트 (재화, 경험치 보상 및 오브젝트 처리를 위한 이벤트)
    public readonly struct EnemyDiedEvent
    {
        public readonly UnityEngine.GameObject enemyGameObject;
        public readonly string enemyId;
        public readonly int rewardGold;
        public readonly int rewardExp;
        public readonly UnityEngine.Vector3 position;

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

    public DataSaveEvent(SaveData saveData)
    {
        this.saveData = saveData;
    }
}

public readonly struct DataLoadEvent
{
    public readonly SaveData saveData;
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

    public GachaDrawCompletedEvent(System.Collections.Generic.List<IGachaRewardItem> resultItems, int currentPityStack)
    {
        this.resultItems = resultItems;
        this.currentPityStack = currentPityStack;
    }
}

#endregion