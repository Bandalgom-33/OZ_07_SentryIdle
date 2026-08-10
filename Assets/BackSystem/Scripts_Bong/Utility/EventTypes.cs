
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