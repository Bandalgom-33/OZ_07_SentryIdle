using System;
using System.Collections.Generic;

// 게임 세이브 데이터 전체 종합 클래스
[Serializable]
public class SaveData
{
    // 보유 재화 데이터
    public CurrencyData currency = new CurrencyData();
    // 재화 및 스탯 업그레이드 레벨 데이터
    public CurrencyUpgradeData statUpgrade = new CurrencyUpgradeData();
    // 스테이지 및 웨이브 진행 데이터
    public StageData stage = new StageData();
    // 보유 유닛 및 덱 배치 정보 데이터
    public UnitDeckData unitDeck = new UnitDeckData();
    // 가챠 천장 및 뽑기 진행 데이터
    public GachaData gacha = new GachaData();
    // 게임 시스템 설정 데이터
    public SettingsData settings = new SettingsData();
    // 마지막 저장 일시 타임스탬프 (오프라인 보상 계산용)
    public string lastSaveTimestamp = string.Empty;
}

// 보유 재화 수량 저장 구조
[Serializable]
public class CurrencyData
{
    // 보유 골드 수량
    public long gold;
    // 보유 다이아 수량
    public long diamond;
    // 보유 웨이브 마석 수량
    public long waveStone;
    // 보유 스테이지 마석 수량
    public long stageStone;
    // 보유 레이드 마석 수량
    public long raidStone;
}

// 재화 및 공통 스탯 업그레이드 레벨 데이터
[Serializable]
public class CurrencyUpgradeData
{
    // 골드 보너스 획득량 업그레이드 레벨
    public int goldBonusLevel;
    // 골드 획득 배율 업그레이드 레벨
    public int goldMagnificationLevel;
    // 다이아 보너스 획득량 업그레이드 레벨
    public int diamondBonusLevel;
    // 다이아 획득 배율 업그레이드 레벨
    public int diamondMagnificationLevel;
    // DP 코스트 보너스 획득량 업그레이드 레벨
    public int dpCostBonusLevel;
    // 최대 DP 코스트 상한 업그레이드 레벨
    public int maxDpCostLevel;

    // 물리 공격력 업그레이드 레벨
    public int physicalAttackLevel;
    // 마법 공격력 업그레이드 레벨
    public int magicalAttackLevel;
    // 최대 체력 업그레이드 레벨
    public int maxHpLevel;
    // 초당 HP 재생 업그레이드 레벨
    public int hpRegenLevel;
    // 물리 방어력 업그레이드 레벨
    public int physicalDefenseLevel;
    // 마법 방어력 업그레이드 레벨
    public int magicalDefenseLevel;
    // 공격 속도 업그레이드 레벨
    public int attackSpeedLevel;
    // 명중력 업그레이드 레벨
    public int accuracyLevel;
    // 회피력 업그레이드 레벨
    public int evasionLevel;
    // 치명타 확률 업그레이드 레벨
    public int criticalChanceLevel;
    // 치명타 피해량 업그레이드 레벨
    public int criticalDamageLevel;

    // 최대 DP 코스트 상한 호환 프로퍼티
    public int dpCostMagnificationLevel
    {
        get => maxDpCostLevel;
        set => maxDpCostLevel = value;
    }
}

// 스테이지 진행 현황 데이터
[Serializable]
public class StageData
{
    // 현재 진행 중인 스테이지 번호
    public int currentStage = 1;
    // 현재 진행 중인 웨이브 번호
    public int currentWave = 1;
    // 도달한 최고 웨이브 번호
    public int maxWave = 1;
}

// 유닛 보유 및 덱 슬롯 데이터
[Serializable]
public class UnitDeckData
{
    // 보유 중인 유닛 세이브 데이터 리스트
    public List<UnitSaveData> ownedUnits = new List<UnitSaveData>();
    // 필드 덱 슬롯 배치 유닛 ID 목록 (총 10개, 미배치 시 -1)
    public int[] deckSlots = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
}

// 개별 유닛 성장 데이터
[Serializable]
public class UnitSaveData
{
    // 유닛 고유 식별 ID
    public int unitId = -1;
    // 유닛 수련 레벨
    public int level = 1;
    // 유닛 현재 누적 경험치량
    public long currentExp = 0L;
    // 유닛 한계 돌파 단계
    public int breakThroughStep = 0;
    // 한계 돌파용 보유 유닛 조각 수
    public int fragmentCount = 0;
}

// 가챠 진행 데이터
[Serializable]
public class GachaData
{
    // 현재 누적 천장 뽑기 횟수
    public int pityStackCount = 0;
}

// 게임 환경 설정 데이터
[Serializable]
public class SettingsData
{
    // 배경음악(BGM) 음량 (0.0 ~ 1.0)
    public float bgmVolume = 1.0f;
    // 효과음(SFX) 음량 (0.0 ~ 1.0)
    public float sfxVolume = 1.0f;
}

