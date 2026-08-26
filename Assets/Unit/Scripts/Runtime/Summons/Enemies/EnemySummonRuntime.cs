using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyRuntimeState))]
    [RequireComponent(typeof(EnemySummonChase))]
    [RequireComponent(typeof(DamageNumberEmitter))]
    [RequireComponent(typeof(HitFlash))]
    [RequireComponent(typeof(HitShake))]
    public sealed class EnemySummonRuntime : MonoBehaviour
    {
        [Header("소환물 수명")]
        [Tooltip("0이면 시간 제한 없이 유지합니다. 0보다 크면 해당 시간이 지난 뒤 풀로 반환됩니다.")]
        [Min(0f)]
        [SerializeField] private float lifetimeSeconds;

        [Tooltip("소환한 몬스터가 사망하면 이 소환물도 함께 제거할지 설정합니다.")]
        [SerializeField] private bool releaseWhenOwnerDies = true;

        [Header("전투 사망 연출")]
        [Tooltip("HP가 0이 된 뒤 마지막 피해 숫자와 피격 반응을 보여주기 위해 풀 반환을 잠시 늦춥니다.")]
        [Min(0f)]
        [SerializeField] private float deathReleaseDelay = 0.14f;

        [Header("소환물 전용 능력치 보정")]
        [Tooltip("기존 EnemyDataSO 전투 능력치를 재사용하면서 소환물에만 필요한 추가 보정을 적용합니다.")]
        [SerializeField] private List<SummonStatModifier> statModifiers = new List<SummonStatModifier>();

        private readonly List<int> appliedModifierIds = new List<int>(4);

        private EnemyRuntimeState state;
        private EnemySummonChase chase;
        private EnemyRuntimeState owner;
        private UnityEngine.Object source;
        private bool initialized;
        private bool releaseRequested;

        public EnemyRuntimeState State => state;
        public EnemyRuntimeState Owner => owner;
        public UnityEngine.Object Source => source;
        public EnemySummonChase Chase => chase;
        public bool IsInitialized => initialized;

        private void Awake()
        {
            state = GetComponent<EnemyRuntimeState>();
            chase = GetComponent<EnemySummonChase>();
        }

        private void OnEnable()
        {
            releaseRequested = false;
        }

        private void OnDisable()
        {
            SummonService.UnregisterEnemySummon(this);
            UnsubscribeOwnerDeath();
            SummonLifetimeRegistry.Unregister(gameObject);
            SummonStatModifierRuntime.Remove(state != null ? state.Stats : null, appliedModifierIds);

            owner = null;
            source = null;
            initialized = false;
            releaseRequested = false;
        }

        public bool InitializeSummon(EnemyRuntimeState sourceOwner, UnityEngine.Object sourceObject)
        {
            if (state == null)
            {
                state = GetComponent<EnemyRuntimeState>();
            }

            if (chase == null)
            {
                chase = GetComponent<EnemySummonChase>();
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

            bool maxHpChanged = SummonStatModifierRuntime.Apply(state.Stats, statModifiers, appliedModifierIds);

            if (maxHpChanged)
            {
                state.Health.SetMaxHp(state.Stats.MaxHp);
                state.Heal(state.Health.MaxHp);
            }

            state.Move.ClearPath();
            transform.position = owner.transform.position;

            CombatTargetLayer targetLayer = state.DataLink.EnemyData.MovementType == EnemyMovementType.Air ? CombatTargetLayer.Air : CombatTargetLayer.Ground;
            state.GridPosition.Initialize(owner.GridPosition.TileCoordinate, owner.GridPosition.FacingDirection, targetLayer);

            initialized = true;
            SummonService.RegisterEnemySummon(this, owner, source);

            if (lifetimeSeconds > 0f)
            {
                SummonLifetimeRegistry.Register(gameObject, lifetimeSeconds);
            }

            return true;
        }

        public void Release()
        {
            if (releaseRequested || !gameObject.activeInHierarchy)
            {
                return;
            }

            releaseRequested = true;

            if (state != null && state.Health != null && state.Health.IsDead && deathReleaseDelay > 0f)
            {
                SummonLifetimeRegistry.Register(gameObject, deathReleaseDelay);
                return;
            }

            SummonService.Release(gameObject);
        }

        private bool CanUseOwner(EnemyRuntimeState sourceOwner)
        {
            return sourceOwner != null && sourceOwner.IsInitialized && sourceOwner.Health != null && !sourceOwner.Health.IsDead && sourceOwner.GridPosition != null && sourceOwner.GridPosition.IsInitialized;
        }

        private bool CanUseSummonState()
        {
            return state != null && state.IsInitialized && state.Health != null && !state.Health.IsDead && state.Stats != null && state.Stats.IsInitialized && state.Move != null && state.GridPosition != null && state.DataLink != null && state.DataLink.HasData && chase != null;
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