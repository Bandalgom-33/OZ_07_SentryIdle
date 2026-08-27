using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitRuntimeState))]
    [RequireComponent(typeof(DamageNumberEmitter))]
    [RequireComponent(typeof(HitFlash))]
    [RequireComponent(typeof(HitShake))]
    public sealed class UnitSummonRuntime : MonoBehaviour
    {
        [Header("소환 위치")]
        [Tooltip("최종 소환 위치에 더할 시각적 월드 좌표 오프셋입니다.")]
        [SerializeField] private Vector3 spawnOffset;

        [Header("소환물 수명")]
        [Tooltip("0이면 시간 제한 없이 유지합니다. 0보다 크면 해당 시간이 지난 뒤 풀로 반환됩니다.")]
        [Min(0f)]
        [SerializeField] private float lifetimeSeconds;

        [Tooltip("소환한 캐릭터가 사망하면 이 소환물도 함께 제거할지 설정합니다.")]
        [SerializeField] private bool releaseWhenOwnerDies = true;

        [Header("소환물 전용 능력치 보정")]
        [Tooltip("소환물 자신의 기준 능력치에 추가로 적용할 고정값과 비율 보정입니다.")]
        [SerializeField] private List<SummonStatModifier> statModifiers = new List<SummonStatModifier>();

        [Header("소환자 능력치 상속")]
        [Tooltip("소환 시점의 소환자 현재 전투 능력치에서 일정 비율을 가져와 소환물에 적용합니다.")]
        [SerializeField] private List<SummonOwnerStatInheritance> ownerStatInheritances = new List<SummonOwnerStatInheritance>();

        private readonly List<int> appliedModifierIds = new List<int>(8);

        private UnitRuntimeState state;
        private UnitRuntimeState owner;
        private UnityEngine.Object source;
        private bool initialized;
        private bool releaseRequested;

        public UnitRuntimeState State => state;
        public UnitRuntimeState Owner => owner;
        public UnityEngine.Object Source => source;
        public bool IsInitialized => initialized;

        private void Awake()
        {
            state = GetComponent<UnitRuntimeState>();
        }

        private void OnEnable()
        {
            releaseRequested = false;
        }

        private void OnDisable()
        {
            UnsubscribeOwnerDeath();
            SummonLifetimeRegistry.Unregister(gameObject);

            if (initialized)
            {
                PassiveRuntimeEvents.NotifyUnitSummonDestroyed(owner, gameObject);
            }

            SummonStatModifierRuntime.Remove(state != null ? state.Stats : null, appliedModifierIds);

            owner = null;
            source = null;
            initialized = false;
            releaseRequested = false;
        }

        public bool InitializeSummon(UnitRuntimeState sourceOwner, UnityEngine.Object sourceObject)
        {
            if (!CanUseOwner(sourceOwner))
            {
                return false;
            }

            SummonTile ownerTile = new SummonTile(sourceOwner.transform.position, sourceOwner.GridPosition.TileCoordinate);
            return InitializeSummonInternal(sourceOwner, sourceObject, ownerTile);
        }

        internal bool InitializeSummon(UnitRuntimeState sourceOwner, UnityEngine.Object sourceObject, SummonTile tile)
        {
            return InitializeSummonInternal(sourceOwner, sourceObject, tile);
        }

        public void Release()
        {
            if (releaseRequested || !gameObject.activeInHierarchy)
            {
                return;
            }

            releaseRequested = true;
            SummonService.Release(gameObject);
        }

        private bool InitializeSummonInternal(UnitRuntimeState sourceOwner, UnityEngine.Object sourceObject, SummonTile tile)
        {
            if (state == null)
            {
                state = GetComponent<UnitRuntimeState>();
            }

            if (!CanUseOwner(sourceOwner) || !CanUseSummonState())
            {
                return false;
            }

            SummonStatModifierRuntime.Remove(state.Stats, appliedModifierIds);
            UnsubscribeOwnerDeath();

            owner = sourceOwner;
            source = sourceObject;
            releaseRequested = false;

            owner.Health.OnDied += HandleOwnerDied;

            transform.position = tile.WorldPosition + spawnOffset;
            state.GridPosition.Initialize(tile.TileCoordinate, owner.GridPosition.FacingDirection, CombatTargetLayer.Ground);

            bool maxHpChanged = SummonStatModifierRuntime.Apply(state.Stats, statModifiers, appliedModifierIds);
            maxHpChanged |= SummonStatModifierRuntime.ApplyOwnerInheritance(state.Stats, owner.Stats, ownerStatInheritances, appliedModifierIds);

            if (maxHpChanged)
            {
                state.SyncHealthMaxHpFromStats();
                state.Heal(state.Health.MaxHp);
            }

            initialized = true;

            if (lifetimeSeconds > 0f)
            {
                SummonLifetimeRegistry.Register(gameObject, lifetimeSeconds);
            }

            PassiveRuntimeEvents.NotifyUnitSummonCreated(owner, gameObject);
            return true;
        }

        private bool CanUseOwner(UnitRuntimeState sourceOwner)
        {
            return sourceOwner != null && sourceOwner.IsInitialized && sourceOwner.Health != null && !sourceOwner.Health.IsDead && sourceOwner.Stats != null && sourceOwner.Stats.IsInitialized && sourceOwner.GridPosition != null && sourceOwner.GridPosition.IsInitialized;
        }

        private bool CanUseSummonState()
        {
            return state != null && state.IsInitialized && state.Health != null && !state.Health.IsDead && state.Stats != null && state.Stats.IsInitialized && state.GridPosition != null;
        }

        private void HandleOwnerDied(CombatHealth diedHealth)
        {
            if (!initialized || !releaseWhenOwnerDies || owner == null || diedHealth == null || diedHealth != owner.Health)
            {
                return;
            }

            Release();
        }

        private void UnsubscribeOwnerDeath()
        {
            if (owner != null && owner.Health != null)
            {
                owner.Health.OnDied -= HandleOwnerDied;
            }
        }
    }
}