using System;
using System.Collections.Generic;
using UnityEngine;

// 게임 세이브 데이터 전체 종합 클래스
[Serializable]
public class SaveData
{
    // ─────────────────────────────────────────────────────────────────────────
    // 세이브 포맷 버전 (SaveManager.requiredSaveVersion과 비교하여 불일치 시 초기화)
    // 필드 추가/구조 변경 시 버전을 올려 구버전 데이터와 충돌을 방지
    // ─────────────────────────────────────────────────────────────────────────
    public int saveVersion = 0;

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
    // 보유 소모품 아이템 수량 데이터 (체력포션 3종, 경험치책 3종)
    public ConsumableSaveData consumable = new ConsumableSaveData();
    // 아이템 조합 공방 상태 데이터 (공장 레벨, 토글 상태, 진행도)
    public CraftingSaveData crafting = new CraftingSaveData();
    // 던전 파견 및 자동 생산 상태 데이터 (3개 던전 유닛 배치 및 진행 시간)
    public DungeonSaveData dungeon = new DungeonSaveData();
    // 가방 인벤토리 아이템 슬롯 데이터 (50칸 그리드)
    public InventorySaveData inventory = new InventorySaveData();
    // 캐릭터별 4부위 장착 장비 데이터
    public EquipmentSaveData equipment = new EquipmentSaveData();
    // 오디오 볼륨 설정 데이터 (BGM 및 SFX)
    public SoundSaveData sound = new SoundSaveData();
    // 신규 유저 가이드 완료 여부
    public bool isGuideCompleted = false;
    // 마지막 저장 일시 타임스탬프 (오프라인 보상 계산용)
    public string lastSaveTimestamp = string.Empty;

    // 세이브 데이터 무결성 검증 및 비정상 수치 보정
    public void Validate()
    {
        ValidateCurrency();
        ValidateUpgrade();
        ValidateStage();
        ValidateGacha();
        ValidateUnits();
        ValidateCrafting();
        ValidateConsumable();
        ValidateInventory();
        ValidateSound();
    }

    // 보유 재화 데이터 유효 범위 검증
    private void ValidateCurrency()
    {
        if (currency == null) { currency = new CurrencyData(); return; }

        currency.gold = ClampMin(currency.gold, 0L, "currency.gold");
        currency.diamond = ClampMin(currency.diamond, 0L, "currency.diamond");
        currency.waveStone = ClampMin(currency.waveStone, 0L, "currency.waveStone");
        currency.stageStone = ClampMin(currency.stageStone, 0L, "currency.stageStone");
        currency.raidStone = ClampMin(currency.raidStone, 0L, "currency.raidStone");
    }

    // 업그레이드 레벨 유효 범위 검증
    private void ValidateUpgrade()
    {
        if (statUpgrade == null) { statUpgrade = new CurrencyUpgradeData(); return; }
        const int maxLevel = 9999;

        statUpgrade.goldBonusLevel = ClampRange(statUpgrade.goldBonusLevel, 0, maxLevel, "statUpgrade.goldBonusLevel");
        statUpgrade.goldMagnificationLevel = ClampRange(statUpgrade.goldMagnificationLevel, 0, maxLevel, "statUpgrade.goldMagnificationLevel");
        statUpgrade.diamondBonusLevel = ClampRange(statUpgrade.diamondBonusLevel, 0, maxLevel, "statUpgrade.diamondBonusLevel");
        statUpgrade.diamondMagnificationLevel = ClampRange(statUpgrade.diamondMagnificationLevel, 0, maxLevel, "statUpgrade.diamondMagnificationLevel");
        statUpgrade.dpCostBonusLevel = ClampRange(statUpgrade.dpCostBonusLevel, 0, maxLevel, "statUpgrade.dpCostBonusLevel");
        statUpgrade.maxDpCostLevel = ClampRange(statUpgrade.maxDpCostLevel, 0, maxLevel, "statUpgrade.maxDpCostLevel");
        statUpgrade.physicalAttackLevel = ClampRange(statUpgrade.physicalAttackLevel, 0, maxLevel, "statUpgrade.physicalAttackLevel");
        statUpgrade.magicalAttackLevel = ClampRange(statUpgrade.magicalAttackLevel, 0, maxLevel, "statUpgrade.magicalAttackLevel");
        statUpgrade.maxHpLevel = ClampRange(statUpgrade.maxHpLevel, 0, maxLevel, "statUpgrade.maxHpLevel");
        statUpgrade.hpRegenLevel = ClampRange(statUpgrade.hpRegenLevel, 0, maxLevel, "statUpgrade.hpRegenLevel");
        statUpgrade.physicalDefenseLevel = ClampRange(statUpgrade.physicalDefenseLevel, 0, maxLevel, "statUpgrade.physicalDefenseLevel");
        statUpgrade.magicalDefenseLevel = ClampRange(statUpgrade.magicalDefenseLevel, 0, maxLevel, "statUpgrade.magicalDefenseLevel");
        statUpgrade.attackSpeedLevel = ClampRange(statUpgrade.attackSpeedLevel, 0, 100, "statUpgrade.attackSpeedLevel");
        statUpgrade.accuracyLevel = ClampRange(statUpgrade.accuracyLevel, 0, maxLevel, "statUpgrade.accuracyLevel");
        statUpgrade.evasionLevel = ClampRange(statUpgrade.evasionLevel, 0, maxLevel, "statUpgrade.evasionLevel");
        statUpgrade.criticalChanceLevel = ClampRange(statUpgrade.criticalChanceLevel, 0, 100, "statUpgrade.criticalChanceLevel");
        statUpgrade.criticalDamageLevel = ClampRange(statUpgrade.criticalDamageLevel, 0, 200, "statUpgrade.criticalDamageLevel");
    }

    // 스테이지 진행 데이터 유효 범위 검증
    private void ValidateStage()
    {
        if (stage == null) { stage = new StageData(); return; }

        stage.currentStage = ClampMin(stage.currentStage, 1, "stage.currentStage");
        stage.currentWave = ClampMin(stage.currentWave, 1, "stage.currentWave");
        stage.maxWave = ClampMin(stage.maxWave, 1, "stage.maxWave");
        if (stage.averageWaveDuration <= 0f)
        {
            Debug.LogWarning("[SaveData] 무결성 보정: stage.averageWaveDuration <= 0 → 15.0 으로 복구");
            stage.averageWaveDuration = 15.0f;
        }
    }

    // 가챠 천장 스택 유효 범위 검증
    private void ValidateGacha()
    {
        if (gacha == null) { gacha = new GachaData(); return; }

        gacha.pityStackCount = ClampRange(gacha.pityStackCount, 0, 100, "gacha.pityStackCount");
    }

    // 보유 유닛 성장 데이터 유효 범위 검증
    private void ValidateUnits()
    {
        if (unitDeck == null) { unitDeck = new UnitDeckData(); return; }
        if (unitDeck.ownedUnits == null) return;

        for (int i = 0; i < unitDeck.ownedUnits.Count; i++)
        {
            UnitSaveData u = unitDeck.ownedUnits[i];
            if (u == null) continue;

            u.level = ClampMin(u.level, 1, $"ownedUnits[{i}].level");
            u.currentExp = ClampMin(u.currentExp, 0L, $"ownedUnits[{i}].currentExp");
            u.breakThroughStep = ClampMin(u.breakThroughStep, 0, $"ownedUnits[{i}].breakThroughStep");
            u.fragmentCount = ClampMin(u.fragmentCount, 0, $"ownedUnits[{i}].fragmentCount");
        }
    }

    // 공방 상태 데이터 유효 범위 검증
    private void ValidateCrafting()
    {
        if (crafting == null) { crafting = new CraftingSaveData(); return; }

        crafting.factoryLevel = ClampRange(crafting.factoryLevel, 1, 5, "crafting.factoryLevel");

        if (crafting.progressEntries != null)
        {
            for (int i = 0; i < crafting.progressEntries.Count; i++)
            {
                if (crafting.progressEntries[i] != null && crafting.progressEntries[i].progress < 0f)
                {
                    crafting.progressEntries[i].progress = 0f;
                }
            }
        }
    }

    // 소모품 수량 데이터 유효 범위 검증
    private void ValidateConsumable()
    {
        if (consumable == null) { consumable = new ConsumableSaveData(); return; }

        consumable.healthPotionLow = ClampMin(consumable.healthPotionLow, 0, "consumable.healthPotionLow");
        consumable.healthPotionMid = ClampMin(consumable.healthPotionMid, 0, "consumable.healthPotionMid");
        consumable.healthPotionHigh = ClampMin(consumable.healthPotionHigh, 0, "consumable.healthPotionHigh");
        consumable.expBookLow = ClampMin(consumable.expBookLow, 0, "consumable.expBookLow");
        consumable.expBookMid = ClampMin(consumable.expBookMid, 0, "consumable.expBookMid");
        consumable.expBookHigh = ClampMin(consumable.expBookHigh, 0, "consumable.expBookHigh");
    }

    // 인벤토리 슬롯 및 수량 유효 범위 검증
    private void ValidateInventory()
    {
        if (inventory == null) { inventory = new InventorySaveData(); return; }
        if (inventory.slots == null) return;

        for (int i = inventory.slots.Count - 1; i >= 0; i--)
        {
            InventorySlotSaveEntry slot = inventory.slots[i];
            if (slot == null) { inventory.slots.RemoveAt(i); continue; }

            if (slot.slotIndex < 0 || slot.slotIndex >= 50)
            {
                Debug.LogWarning($"[SaveData] 무결성 보정: inventory.slots[{i}].slotIndex={slot.slotIndex} 범위 이탈 → 항목 제거");
                inventory.slots.RemoveAt(i);
                continue;
            }
            if (slot.quantity < 0)
            {
                Debug.LogWarning($"[SaveData] 무결성 보정: inventory.slots[{i}].quantity={slot.quantity} 음수 → 0");
                slot.quantity = 0;
            }
        }
    }

    // 오디오 볼륨 설정 유효 범위(0.0~1.0) 검증
    private void ValidateSound()
    {
        if (sound == null) { sound = new SoundSaveData(); return; }

        sound.bgmVolume = Mathf.Clamp01(sound.bgmVolume);
        sound.sfxVolume = Mathf.Clamp01(sound.sfxVolume);
    }

    // 정수형(long) 최소값 하한 보정
    private static long ClampMin(long value, long min, string fieldName)
    {
        if (value < min)
        {
            Debug.LogWarning($"[SaveData] 무결성 보정: {fieldName} = {value} → {min}");
            return min;
        }
        return value;
    }

    // 정수형(int) 최소값 하한 보정
    private static int ClampMin(int value, int min, string fieldName)
    {
        if (value < min)
        {
            Debug.LogWarning($"[SaveData] 무결성 보정: {fieldName} = {value} → {min}");
            return min;
        }
        return value;
    }

    // 정수형(int) 유효 범위(최소~최대) 보정
    private static int ClampRange(int value, int min, int max, string fieldName)
    {
        if (value < min)
        {
            Debug.LogWarning($"[SaveData] 무결성 보정: {fieldName} = {value} → {min}");
            return min;
        }
        if (value > max)
        {
            Debug.LogWarning($"[SaveData] 무결성 보정: {fieldName} = {value} → {max}");
            return max;
        }
        return value;
    }
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

    // 최근 5개 웨이브 클리어 소요 시간 기록 목록 (초 단위)
    public List<float> recentWaveDurations = new List<float>();
    // 최근 5개 웨이브 클리어 소요 시간 이동 평균 (기본값: 15.0초)
    public float averageWaveDuration = 15.0f;
}

// 유닛 보유 및 멀티 덱(일반 1개, 레이드 2개) 슬롯 데이터
[Serializable]
public class UnitDeckData
{
    // 기본 보유 유닛 세이브 데이터 리스트 (루카: 1성 ID 2, 김하진: 2성 ID 4 기본 지급, 0돌파)
    public List<UnitSaveData> ownedUnits = new List<UnitSaveData>
    {
        // 1성 뱅가드 루카 (UNIT_0002, 0돌파 기본 보유)
        new UnitSaveData { unitId = 2, level = 1, currentExp = 0L, breakThroughStep = 0, fragmentCount = 0 },
        // 2성 가드 김하진 (UNIT_0004, 0돌파 기본 보유)
        new UnitSaveData { unitId = 4, level = 1, currentExp = 0L, breakThroughStep = 0, fragmentCount = 0 }
    };

    // 일반 필드 덱 슬롯 배치 유닛 ID 목록 (1번: 루카 ID 2, 2번: 김하진 ID 4, 미배치 시 -1)
    public int[] normalDeckSlots = new int[] { 2, 4, -1, -1, -1, -1, -1, -1, -1, -1 };

    // 레이드 1팀 덱 슬롯 배치 유닛 ID 목록 (미배치 시 -1)
    public int[] raid1DeckSlots = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    // 레이드 2팀 덱 슬롯 배치 유닛 ID 목록 (미배치 시 -1)
    public int[] raid2DeckSlots = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    // 기존 단일 덱 세이브 데이터 및 레거시 코드 하위 호환용 프로퍼티
    public int[] deckSlots
    {
        get => normalDeckSlots;
        set => normalDeckSlots = value;
    }
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

// 가챠 뽑기 개별 로그 데이터 DTO (시간 및 유닛 인덱스만 경량화하여 저장)
[Serializable]
public class GachaLogEntry
{
    // 획득 유닛의 정수 ID (예: 2 -> UNIT_0002)
    public int unitId;
    // 뽑은 시점 타임스탬프 (HH:mm:ss 포맷)
    public string timestamp = string.Empty;
}

// 가챠 진행 데이터
[Serializable]
public class GachaData
{
    // 현재 누적 천장 뽑기 횟수
    public int pityStackCount = 0;
    // 최근 가챠 이력 로그 목록 (최대 100개 기록 보관)
    public List<GachaLogEntry> drawLogs = new List<GachaLogEntry>();
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

// 6종 소모품 아이템 보유 수량 저장 데이터
[Serializable]
public class ConsumableSaveData
{
    // 하급 체력포션 (HP 25% 회복) 보유 수량
    public int healthPotionLow;
    // 중급 체력포션 (HP 50% 회복) 보유 수량
    public int healthPotionMid;
    // 상급 체력포션 (HP 100% 회복) 보유 수량
    public int healthPotionHigh;

    // 초급 경험치책 (+100 EXP) 보유 수량
    public int expBookLow;
    // 중급 경험치책 (+1,000 EXP) 보유 수량
    public int expBookMid;
    // 고급 경험치책 (+10,000 EXP) 보유 수량
    public int expBookHigh;
}

// 개별 레시피 진행도 저장 데이터 DTO
[Serializable]
public class RecipeProgressSaveEntry
{
    public string recipeId = string.Empty;
    public float progress = 0.0f;
}

// 공방 상태 저장 데이터
[Serializable]
public class CraftingSaveData
{
    public int factoryLevel = 1;
    public bool isGlobalAutoEnabled = false;
    public List<int> queuedRecipeIndices = new List<int>();
    public List<string> queuedRecipeIds = new List<string>();
    public List<RecipeProgressSaveEntry> progressEntries = new List<RecipeProgressSaveEntry>();
    public List<float> recipeProgresses = new List<float>();
}

// 개별 던전 슬롯 파견 상태 저장 데이터
[Serializable]
public class DungeonSlotSaveData
{
    // 대상 던전 식별 ID (예: DUNGEON_01, DUNGEON_02, DUNGEON_03)
    public string dungeonId = string.Empty;
    // 해당 던전에 파견된 3개 유닛 고유 ID 배열 (-1: 미배치)
    public int[] assignedUnitIds = new int[3] { -1, -1, -1 };
    // 현재 생산 주기 진행 시간 (초)
    public float currentCycleTimer = 0.0f;
}

// 던전 전체 파견 및 방치 생산 저장 데이터
[Serializable]
public class DungeonSaveData
{
    // 3개 던전 슬롯의 저장 데이터 리스트
    public List<DungeonSlotSaveData> dungeonSlots = new List<DungeonSlotSaveData>();
}

// 인벤토리 단일 슬롯 아이템 저장 데이터 DTO
[Serializable]
public class InventorySlotSaveEntry
{
    // 50칸 그리드 내 슬롯 인덱스 (0 ~ 49)
    public int slotIndex;
    // 아이템 고유 식별자 문자열 (ItemDataSO.ItemID)
    public string itemId = string.Empty;
    // 보유 수량
    public int quantity = 1;
}

// 가방 인벤토리 전체 저장 데이터
[Serializable]
public class InventorySaveData
{
    // 아이템이 존재하는 슬롯 목록
    public List<InventorySlotSaveEntry> slots = new List<InventorySlotSaveEntry>();
}

// 개별 캐릭터의 4부위(머리, 갑옷, 무기, 장신구) 장착 장비 저장 데이터 DTO
[Serializable]
public class CharacterEquipmentSaveEntry
{
    // 유닛 고유 식별자 문자열 (예: UNIT_0002, UNIT_0004)
    public string unitId = string.Empty;
    // 투구(Head) 아이템 ID (미장착 시 empty)
    public string headItemId = string.Empty;
    // 갑옷(Armor) 아이템 ID
    public string armorItemId = string.Empty;
    // 무기(Weapon) 아이템 ID
    public string weaponItemId = string.Empty;
    // 장신구(Accessory) 아이템 ID
    public string accessoryItemId = string.Empty;
}

// 전체 캐릭터 장비 장착 상태 저장 데이터
[Serializable]
public class EquipmentSaveData
{
    // 캐릭터별 장비 장착 데이터 목록
    public List<CharacterEquipmentSaveEntry> characterEquipments = new List<CharacterEquipmentSaveEntry>();
}

// 오디오 볼륨 설정 저장 데이터
[Serializable]
public class SoundSaveData
{
    // 배경음악 볼륨 (0.0 ~ 1.0)
    public float bgmVolume = 1.0f;
    // 효과음 볼륨 (0.0 ~ 1.0)
    public float sfxVolume = 1.0f;
}
