using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace EndlessGuard.Unit.Runtime
{
    internal sealed class ExplosionHandler : IEnemyDiedPassiveHandler
    {
        public Type DataType => typeof(ExplosionSO);

        public void OnDied(EnemyRuntimeState owner, PassiveDataSO passive, PassiveTuning tuning)
        {
            if (owner == null || owner.GridPosition == null || !owner.GridPosition.IsInitialized || owner.Attack == null || owner.Attack.DamageRule == null)
            {
                return;
            }

            ExplosionSO data = passive as ExplosionSO;

            if (data == null)
            {
                return;
            }

            float explosionDamage = tuning != null ? tuning.GetValue(PassiveValueKey.ExplosionDamage) : data.ExplosionDamage;
            float explosionRadiusTiles = tuning != null ? tuning.GetValue(PassiveValueKey.ExplosionRadiusTiles) : data.ExplosionRadiusTiles;

            if (float.IsNaN(explosionDamage) || float.IsInfinity(explosionDamage) || explosionDamage <= 0f)
            {
                return;
            }

            if (float.IsNaN(explosionRadiusTiles) || float.IsInfinity(explosionRadiusTiles) || explosionRadiusTiles < 0f)
            {
                return;
            }

            if (data.DamageType != DamageType.Physical && data.DamageType != DamageType.Magical)
            {
                return;
            }

            Vector2Int centerTile = owner.GridPosition.TileCoordinate;
            int radius = Mathf.CeilToInt(explosionRadiusTiles);
            List<UnitRuntimeState> targets = ListPool<UnitRuntimeState>.Get();

            try
            {
                CollectTargets(centerTile, radius, explosionRadiusTiles, targets);
                ApplyDamage(targets, explosionDamage, data.DamageType, owner.Attack.DamageRule);
            }
            finally
            {
                ListPool<UnitRuntimeState>.Release(targets);
            }
        }

        private static void CollectTargets(Vector2Int centerTile, int radius, float explosionRadiusTiles, List<UnitRuntimeState> targets)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) > explosionRadiusTiles)
                    {
                        continue;
                    }

                    Vector2Int tile = centerTile + new Vector2Int(x, y);

                    if (!CombatRegistry.TryGetUnitsAt(tile, out HashSet<UnitRuntimeState> tileUnits))
                    {
                        continue;
                    }

                    foreach (UnitRuntimeState unit in tileUnits)
                    {
                        if (IsValidTarget(unit))
                        {
                            targets.Add(unit);
                        }
                    }
                }
            }
        }

        private static void ApplyDamage(List<UnitRuntimeState> targets, float explosionDamage, DamageType damageType, DamageRuleSO damageRule)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                UnitRuntimeState target = targets[i];

                if (!IsValidTarget(target))
                {
                    continue;
                }

                float defense = damageType == DamageType.Physical ? target.Stats.PhysicalDefense : target.Stats.MagicalDefense;
                float finalDamage = DamageCalculator.Calculate(explosionDamage, defense, damageRule);

                if (finalDamage <= 0f)
                {
                    continue;
                }

                target.ApplyDamage(new DamageInfo(finalDamage, damageType, false));
            }
        }

        private static bool IsValidTarget(UnitRuntimeState target)
        {
            return target != null && target.IsInitialized && target.Health != null && !target.Health.IsDead && target.Stats != null && target.Stats.IsInitialized && target.GridPosition != null && target.GridPosition.IsInitialized;
        }
    }
}