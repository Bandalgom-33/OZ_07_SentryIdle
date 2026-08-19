using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

// 6종 소모품의 인벤토리 수량 관리 및 인게임 사용 효과(HP 회복/경험치 부여) 실행 매니저
public class ConsumableItemManager : SingletonBase<ConsumableItemManager>
{
    #region 내부 변수 모음

    // 6종 소모품의 보유 수량 배열 (인덱스: ConsumableType)
    private readonly int[] _itemCounts = new int[6];

    // 포션 재사용 쿨타임 타이머
    private float _potionCooldownTimer = 0f;
    private const float PotionCooldownDuration = 5.0f;

    #endregion

    #region 이벤트

    // 소모품 보유 수량 변동 실시간 브로드캐스트 이벤트 (타입, 현재수량)
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
        int index = (int)type;
        if (index >= 0 && index < _itemCounts.Length)
        {
            return _itemCounts[index];
        }
        return 0;
    }

    // 소모품 획득 및 수량 가산 처리
    public void AddConsumable(ConsumableType type, int amount = 1)
    {
        if (amount <= 0) return;

        int index = (int)type;
        if (index >= 0 && index < _itemCounts.Length)
        {
            _itemCounts[index] += amount;
            OnConsumableCountChanged?.Invoke(type, _itemCounts[index]);
            Debug.Log($"[ConsumableItemManager] 소모품 획득: {type} +{amount}개 (현재: {_itemCounts[index]}개)");
        }
    }

    // 소모품 보유 검증 및 차감 처리
    public bool TrySpendConsumable(ConsumableType type, int amount = 1)
    {
        int index = (int)type;
        if (index < 0 || index >= _itemCounts.Length || _itemCounts[index] < amount)
        {
            return false;
        }

        _itemCounts[index] -= amount;
        OnConsumableCountChanged?.Invoke(type, _itemCounts[index]);
        return true;
    }

    #endregion

    #region 아이템 인게임 효과 실행 메서드

    // 전체 필드 아군 유닛 HP 동시 회복 실행 (체력포션 3종)
    public bool UseHealthPotion(ConsumableType type)
    {
        if (type != ConsumableType.HealthPotion_Low &&
            type != ConsumableType.HealthPotion_Mid &&
            type != ConsumableType.HealthPotion_High)
        {
            Debug.LogWarning($"[ConsumableItemManager] {type}은(는) 체력 포션이 아닙니다.");
            return false;
        }

        if (!IsPotionReady)
        {
            Debug.LogWarning($"[ConsumableItemManager] 포션 쿨타임 대기 중입니다. (남은 시간: {PotionRemainingCooldown:F1}초)");
            return false;
        }

        if (!TrySpendConsumable(type, 1))
        {
            Debug.LogWarning($"[ConsumableItemManager] {type} 보유 수량이 부족합니다.");
            return false;
        }

        float healRatio = type switch
        {
            ConsumableType.HealthPotion_Low => 0.25f,
            ConsumableType.HealthPotion_Mid => 0.50f,
            ConsumableType.HealthPotion_High => 1.00f,
            _ => 0.25f
        };

        int healedUnitCount = 0;

        foreach (UnitRuntimeState unit in CombatRegistry.Units)
        {
            if (unit == null) continue;

            CombatHealth health = unit.Health != null ? unit.Health : unit.GetComponent<CombatHealth>();
            if (health != null && health.IsInitialized && !health.IsDead)
            {
                float healAmount = health.MaxHp * healRatio;
                health.Heal(healAmount);
                healedUnitCount++;
            }
        }

        _potionCooldownTimer = PotionCooldownDuration;
        SaveManager.Instance.SaveGameData();

        Debug.Log($"[ConsumableItemManager] {type} 사용 완료: 필드 아군 {healedUnitCount}명 HP {healRatio * 100}% 회복");
        return true;
    }

    // 지정 유닛 즉시 경험치 부여 실행 (경험치책 3종)
    public bool UseExpBook(ConsumableType type, string unitId)
    {
        if (type != ConsumableType.ExpBook_Low &&
            type != ConsumableType.ExpBook_Mid &&
            type != ConsumableType.ExpBook_High)
        {
            Debug.LogWarning($"[ConsumableItemManager] {type}은(는) 경험치 책이 아닙니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(unitId))
        {
            Debug.LogWarning("[ConsumableItemManager] 경험치를 부여할 대상 유닛 ID가 비어 있습니다.");
            return false;
        }

        if (!TrySpendConsumable(type, 1))
        {
            Debug.LogWarning($"[ConsumableItemManager] {type} 보유 수량이 부족합니다.");
            return false;
        }

        long expReward = type switch
        {
            ConsumableType.ExpBook_Low => 100L,
            ConsumableType.ExpBook_Mid => 1000L,
            ConsumableType.ExpBook_High => 10000L,
            _ => 100L
        };

        bool success = ExperienceManager.Instance.AddExperienceToUnit(unitId, expReward);

        if (success)
        {
            SaveManager.Instance.SaveGameData();
            Debug.Log($"[ConsumableItemManager] {type} 사용 완료: [{unitId}] 유닛에게 +{expReward} EXP 지급");
        }
        else
        {
            AddConsumable(type, 1);
            Debug.LogWarning($"[ConsumableItemManager] [{unitId}] 유닛에게 경험치 지급 실패하여 아이템을 환불합니다.");
        }

        return success;
    }

    #endregion

    #region 세이브 / 로드 연동

    // 세이브 데이터에 소모품 수량 저장
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;

        evt.saveData.consumable.healthPotionLow = _itemCounts[(int)ConsumableType.HealthPotion_Low];
        evt.saveData.consumable.healthPotionMid = _itemCounts[(int)ConsumableType.HealthPotion_Mid];
        evt.saveData.consumable.healthPotionHigh = _itemCounts[(int)ConsumableType.HealthPotion_High];

        evt.saveData.consumable.expBookLow = _itemCounts[(int)ConsumableType.ExpBook_Low];
        evt.saveData.consumable.expBookMid = _itemCounts[(int)ConsumableType.ExpBook_Mid];
        evt.saveData.consumable.expBookHigh = _itemCounts[(int)ConsumableType.ExpBook_High];
    }

    // 세이브 데이터로부터 소모품 수량 복원 및 이벤트 브로드캐스트
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.consumable == null) return;

        _itemCounts[(int)ConsumableType.HealthPotion_Low] = evt.saveData.consumable.healthPotionLow;
        _itemCounts[(int)ConsumableType.HealthPotion_Mid] = evt.saveData.consumable.healthPotionMid;
        _itemCounts[(int)ConsumableType.HealthPotion_High] = evt.saveData.consumable.healthPotionHigh;

        _itemCounts[(int)ConsumableType.ExpBook_Low] = evt.saveData.consumable.expBookLow;
        _itemCounts[(int)ConsumableType.ExpBook_Mid] = evt.saveData.consumable.expBookMid;
        _itemCounts[(int)ConsumableType.ExpBook_High] = evt.saveData.consumable.expBookHigh;

        for (int i = 0; i < _itemCounts.Length; i++)
        {
            OnConsumableCountChanged?.Invoke((ConsumableType)i, _itemCounts[i]);
        }
    }

    // 소모품 데이터 초기화 및 이벤트 브로드캐스트
    private void OnReset(DataResetEvent evt)
    {
        Array.Clear(_itemCounts, 0, _itemCounts.Length);
        _potionCooldownTimer = 0f;

        for (int i = 0; i < _itemCounts.Length; i++)
        {
            OnConsumableCountChanged?.Invoke((ConsumableType)i, 0);
        }
    }

    #endregion
}
