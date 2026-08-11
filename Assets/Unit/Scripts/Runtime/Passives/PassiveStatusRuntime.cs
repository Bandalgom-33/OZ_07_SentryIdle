using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [Serializable]
    public sealed class PassiveStatusRuntime
    {
        private readonly List<Effect> effects = new List<Effect>(4);
        private RuntimeStats stats;

        public int ActiveEffectCount => effects.Count;

        internal void Initialize(RuntimeStats targetStats)
        {
            Clear();
            stats = targetStats;
        }

        internal void Step(float deltaTime)
        {
            if (deltaTime <= 0f || effects.Count == 0)
            {
                return;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                Effect effect = effects[i];

                if (float.IsPositiveInfinity(effect.RemainingSeconds))
                {
                    continue;
                }

                effect.RemainingSeconds -= deltaTime;

                if (effect.RemainingSeconds > 0f)
                {
                    effects[i] = effect;
                    continue;
                }

                RemoveAt(i);
            }
        }

        internal bool ApplyTimedModifier(UnityEngine.Object source, PassiveDataSO passive, PassiveStatType statType, float flatBonus, float percentBonus, float durationSeconds, bool isNegative)
        {
            if (stats == null || !stats.IsInitialized || passive == null || statType == PassiveStatType.None || durationSeconds <= 0f)
            {
                return false;
            }

            return ApplyModifier(source, passive, statType, flatBonus, percentBonus, durationSeconds, isNegative);
        }

        internal bool ApplyPersistentModifier(UnityEngine.Object source, PassiveDataSO passive, PassiveStatType statType, float flatBonus, float percentBonus, bool isNegative)
        {
            if (stats == null || !stats.IsInitialized || passive == null || statType == PassiveStatType.None)
            {
                return false;
            }

            return ApplyModifier(source, passive, statType, flatBonus, percentBonus, float.PositiveInfinity, isNegative);
        }

        internal bool RemoveModifier(UnityEngine.Object source, PassiveDataSO passive, PassiveStatType statType)
        {
            int sourceId = source != null ? source.GetInstanceID() : 0;
            int passiveId = passive != null ? passive.GetInstanceID() : 0;

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                Effect effect = effects[i];

                if (effect.SourceId != sourceId || effect.PassiveId != passiveId || effect.StatType != statType)
                {
                    continue;
                }

                RemoveAt(i);
                return true;
            }

            return false;
        }

        internal int CleanseNegative()
        {
            int removedCount = 0;

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (!effects[i].IsNegative)
                {
                    continue;
                }

                RemoveAt(i);
                removedCount++;
            }

            return removedCount;
        }

        internal void Clear()
        {
            if (stats != null)
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    int modifierId = effects[i].ModifierId;

                    if (modifierId != 0)
                    {
                        stats.RemoveModifier(modifierId);
                    }
                }
            }

            effects.Clear();
            stats = null;
        }

        private bool ApplyModifier(UnityEngine.Object source, PassiveDataSO passive, PassiveStatType statType, float flatBonus, float percentBonus, float remainingSeconds, bool isNegative)
        {
            int sourceId = source != null ? source.GetInstanceID() : 0;
            int passiveId = passive.GetInstanceID();

            for (int i = 0; i < effects.Count; i++)
            {
                Effect effect = effects[i];

                if (effect.SourceId != sourceId || effect.PassiveId != passiveId || effect.StatType != statType)
                {
                    continue;
                }

                if (effect.ModifierId == 0 || !stats.UpdateModifier(effect.ModifierId, flatBonus, percentBonus))
                {
                    effect.ModifierId = stats.AddModifier(statType, flatBonus, percentBonus);
                }

                effect.RemainingSeconds = remainingSeconds;
                effect.IsNegative = isNegative;
                effects[i] = effect;
                return effect.ModifierId != 0;
            }

            int newModifierId = stats.AddModifier(statType, flatBonus, percentBonus);

            if (newModifierId == 0)
            {
                return false;
            }

            effects.Add(new Effect(sourceId, passiveId, statType, newModifierId, remainingSeconds, isNegative));
            return true;
        }

        private void RemoveAt(int index)
        {
            if (index < 0 || index >= effects.Count)
            {
                return;
            }

            int modifierId = effects[index].ModifierId;

            if (stats != null && modifierId != 0)
            {
                stats.RemoveModifier(modifierId);
            }

            effects.RemoveAt(index);
        }

        private struct Effect
        {
            public int SourceId;
            public int PassiveId;
            public PassiveStatType StatType;
            public int ModifierId;
            public float RemainingSeconds;
            public bool IsNegative;

            public Effect(int sourceId, int passiveId, PassiveStatType statType, int modifierId, float remainingSeconds, bool isNegative)
            {
                SourceId = sourceId;
                PassiveId = passiveId;
                StatType = statType;
                ModifierId = modifierId;
                RemainingSeconds = remainingSeconds;
                IsNegative = isNegative;
            }
        }
    }
}
