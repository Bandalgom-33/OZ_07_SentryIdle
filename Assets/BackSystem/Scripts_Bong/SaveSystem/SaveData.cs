using System;
using System.Collections.Generic;


// 저장 데이터 종합된 데이터 
[Serializable]
public class SaveData
{
    public CurrencyData currency = new CurrencyData();
    public CurrencyUpgradeData statUpgrade = new CurrencyUpgradeData();
    public StageData stage = new StageData();
    public UnitDeckData unitDeck = new UnitDeckData();
    public GachaData gacha = new GachaData();
    public SettingsData settings = new SettingsData();
    // 오프라인 재화 관리용 시간 데이터 
    public string lastSaveTimestamp = string.Empty;
}

// 보유 재화 데이터 
[Serializable]
public class CurrencyData
{
    public long gold;
    public int diamond;
}

// 재화 업그레이드 데이터
[Serializable]
public class CurrencyUpgradeData
{
    public int goldBonusLevel;
    public int goldMagnificationLevel;
    public int diamondBonusLevel;
    public int diamondMagnificationLevel;
    public int dpCostBonusLevel;
    public int dpCostMagnificationLevel;
}

// 웨이브 데이터 저장
[Serializable]
public class StageData
{
    public int currentStage = 1;
    public int currentWave = 1;
    public int maxWave = 1;
}

// 유닛 덱 정보 
[Serializable]
public class UnitDeckData
{
    // 보유 유닛
    public List<UnitSaveData> ownedUnits = new List<UnitSaveData>();
    // 덱 슬롯 배치 유닛 ID 목록 (최대 10개, 미배치 슬롯은 -1)
    public int[] deckSlots = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
}

// 유닛 개별 목록
[Serializable]
public class UnitSaveData
{
    public int unitId = -1;
    public int level = 1;
    // 돌파 레벨
    public int breakThroughStep = 0;
    // 한계돌파용 보유 조각 수
    public int fragmentCount = 0;
}

// 가챠 데이터
[Serializable]
public class GachaData
{
    // 가챠 천장 누적 뽑기 횟수
    public int pityStackCount = 0;
}

// 옵션 데이터 
[Serializable]
public class SettingsData
{
    // BGM 볼륨 (0.0 ~ 1.0)
    public float bgmVolume = 1.0f;
    // SFX 효과음 볼륨 (0.0 ~ 1.0)
    public float sfxVolume = 1.0f;
}

