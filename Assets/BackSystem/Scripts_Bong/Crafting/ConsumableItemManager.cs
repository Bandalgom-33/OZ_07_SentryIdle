using System;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

// 6종 소모품의 인벤토리 수량 관리 및 인게임 사용 효과(HP 회복/경험치 부여) 실행 매니저
public class ConsumableItemManager : SingletonBase<ConsumableItemManager>
{
    #region 내부 변수 모음

    private readonly int[] _itemCounts = new int[6];
    private float _potionCooldownTimer = 0f;
    private const float PotionCooldownDuration = 5.0f;

    #endregion

    #region 이벤트

    public static event Action<ConsumableType, int> OnConsumableCountChanged;

    #endregion

    #region 프로퍼티

    public bool IsPotionReady => _potionCooldownTimer <= 0f;
    public float PotionRemainingCooldown => Mathf.Max(0f, _potionCooldownTimer);

    #endregion

    #region 라이프사이클 및 세이브 연동

    // 이벤트 버스 구독 등록
    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    // 포션 쿨타임 실시간 감소 갱신
    private void Update()
    {
        if (_potionCooldownTimer > 0f)
        {
            _potionCooldownTimer -= Time.deltaTime;
        }
    }

    #endregion

    #region 아이템 보유 조회 및 조작 메서드

    // 특정 소모품의 현재 보유 수량 반환
    public int GetItemCount(ConsumableType type)
    {
        if (InventoryGridManager.Instance != null)
        {
            return InventoryGridManager.Instance.GetConsumableCount(type);
        }
        return 0;
    }

    // 소모품 획득 및 인벤토리 가산 처리
    public void AddConsumable(ConsumableType type, int amount = 1)
    {
        if (amount <= 0 || InventoryGridManager.Instance == null) return;

        ItemDataSO itemData = InventoryGridManager.Instance.GetConsumableItemData(type);
        if (itemData != null)
        {
            InventoryGridManager.Instance.AddItem(itemData, amount);
            OnConsumableCountChanged?.Invoke(type, GetItemCount(type));
        }
    }

    // 소모품 인벤토리 보유 검증 및 차감 처리
    public bool TrySpendConsumable(ConsumableType type, int amount = 1)
    {
        if (InventoryGridManager.Instance == null) return false;

        bool spent = InventoryGridManager.Instance.TrySpendConsumable(type, amount);
        if (spent)
        {
            OnConsumableCountChanged?.Invoke(type, GetItemCount(type));
        }
        return spent;
    }

    #endregion

    #region 아이템 인게임 효과 실행 메서드

    // 전체 필드 아군 유닛 HP 동시 회복 실행 연산 (체력포션 3종)
    public bool UseHealthPotion(ConsumableType type)
    {
        if (!IsHealthPotion(type))
        {
            Debug.LogWarning($"[ConsumableItemManager] {type}은(는) 체력 포션이 아닙니다.");
            return false;
        }

        if (!IsPotionReady)
        {
            Debug.LogWarning($"[ConsumableItemManager] 포션 쿨타임 대기 중입니다. (남은 시간: {PotionRemainingCooldown:F1}초)");
            return false;
        }

        ItemDataSO itemData = InventoryGridManager.Instance?.GetConsumableItemData(type);
        float healRatio = itemData != null && itemData.RecoveryRatio > 0f ? itemData.RecoveryRatio : type switch
        {
            ConsumableType.HealthPotion_Low => 0.25f,
            ConsumableType.HealthPotion_Mid => 0.50f,
            ConsumableType.HealthPotion_High => 1.00f,
            _ => 0.25f
        };

        if (!TrySpendConsumable(type, 1))
        {
            Debug.LogWarning($"[ConsumableItemManager] {type} 보유 수량이 부족합니다.");
            return false;
        }

        foreach (UnitRuntimeState unit in CombatRegistry.Units)
        {
            if (unit == null) continue;

            CombatHealth health = unit.Health != null ? unit.Health : unit.GetComponent<CombatHealth>();
            if (health != null && health.IsInitialized && !health.IsDead)
            {
                float healAmount = health.MaxHp * healRatio;
                health.Heal(healAmount);
            }
        }

        _potionCooldownTimer = PotionCooldownDuration;
        EventBus.Publish(new RequestSaveGameEvent(force: false));
        return true;
    }

    // 지정 유닛 즉시 경험치 부여 실행 연산 (경험치책 3종)
    public bool UseExpBook(ConsumableType type, string unitId)
    {
        if (!IsExpBook(type))
        {
            Debug.LogWarning($"[ConsumableItemManager] {type}은(는) 경험치 책이 아닙니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        if (!TrySpendConsumable(type, 1))
        {
            return false;
        }

        ItemDataSO itemData = InventoryGridManager.Instance?.GetConsumableItemData(type);
        long expReward = itemData != null && itemData.ExpAmount > 0L ? itemData.ExpAmount : type switch
        {
            ConsumableType.ExpBook_Low => 100L,
            ConsumableType.ExpBook_Mid => 1000L,
            ConsumableType.ExpBook_High => 10000L,
            _ => 100L
        };

        bool success = ExperienceManager.Instance != null && ExperienceManager.Instance.AddExperienceToUnit(unitId, expReward);

        if (success)
        {
            EventBus.Publish(new RequestSaveGameEvent(force: false));
        }
        else
        {
            AddConsumable(type, 1);
        }

        return success;
    }

    // 체력 포션 타입 여부 판정 헬퍼
    private bool IsHealthPotion(ConsumableType type)
    {
        return type == ConsumableType.HealthPotion_Low ||
               type == ConsumableType.HealthPotion_Mid ||
               type == ConsumableType.HealthPotion_High;
    }

    // 경험치 책 타입 여부 판정 헬퍼
    private bool IsExpBook(ConsumableType type)
    {
        return type == ConsumableType.ExpBook_Low ||
               type == ConsumableType.ExpBook_Mid ||
               type == ConsumableType.ExpBook_High;
    }

    #endregion

    #region 세이브 / 로드 연동

    // 전체 소모품 수량 변경 이벤트 일괄 브로드캐스트 헬퍼
    private void BroadcastAllCounts()
    {
        for (int i = 0; i < _itemCounts.Length; i++)
        {
            OnConsumableCountChanged?.Invoke((ConsumableType)i, _itemCounts[i]);
        }
    }

    // 세이브 데이터 저장 처리
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.consumable == null)
        {
            evt.saveData.consumable = new ConsumableSaveData();
        }

        evt.saveData.consumable.healthPotionLow = _itemCounts[(int)ConsumableType.HealthPotion_Low];
        evt.saveData.consumable.healthPotionMid = _itemCounts[(int)ConsumableType.HealthPotion_Mid];
        evt.saveData.consumable.healthPotionHigh = _itemCounts[(int)ConsumableType.HealthPotion_High];

        evt.saveData.consumable.expBookLow = _itemCounts[(int)ConsumableType.ExpBook_Low];
        evt.saveData.consumable.expBookMid = _itemCounts[(int)ConsumableType.ExpBook_Mid];
        evt.saveData.consumable.expBookHigh = _itemCounts[(int)ConsumableType.ExpBook_High];
    }

    // 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.consumable == null) return;

        _itemCounts[(int)ConsumableType.HealthPotion_Low] = evt.saveData.consumable.healthPotionLow;
        _itemCounts[(int)ConsumableType.HealthPotion_Mid] = evt.saveData.consumable.healthPotionMid;
        _itemCounts[(int)ConsumableType.HealthPotion_High] = evt.saveData.consumable.healthPotionHigh;

        _itemCounts[(int)ConsumableType.ExpBook_Low] = evt.saveData.consumable.expBookLow;
        _itemCounts[(int)ConsumableType.ExpBook_Mid] = evt.saveData.consumable.expBookMid;
        _itemCounts[(int)ConsumableType.ExpBook_High] = evt.saveData.consumable.expBookHigh;

        BroadcastAllCounts();
    }

    // 소모품 데이터 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        Array.Clear(_itemCounts, 0, _itemCounts.Length);
        _potionCooldownTimer = 0f;

        BroadcastAllCounts();
    }

    #endregion
}
