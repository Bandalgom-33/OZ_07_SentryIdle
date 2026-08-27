using System.Collections;
using System.Collections.Generic;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaidBattleController))]
    [RequireComponent(typeof(RaidBoardRuntime))]
    public sealed class RaidBossLightningRuntime : MonoBehaviour
    {
        private const int MapStrikeCount = 25;
        private const int MapSoundStep = 3;
        private const float StrikeOffsetY = 0.03f;
        private const float TargetTravelScale = 0.78f;
        private const float MapTravelScale = 0.48f;
        private const float TargetImpactScale = 2f;
        private const float MapImpactScale = 1.32f;
        private const float TargetSkyHeight = 23.5f;
        private const float MapSkyHeight = TargetSkyHeight;
        private const float TargetBoltWidth = 0.78f;
        private const float MapBoltWidth = 0.32f;
        private const float TravelSimulationSpeed = 1.35f;
        private const float ImpactSimulationSpeed = 0.92f;
        private const float ImpactLifetime = 0.95f;
        private const float MapStrikeStartDelay = 0.08f;
        private const float MapStrikeInterval = 0.2f;

        private static readonly WaitForSeconds MapStrikeStartWait = new WaitForSeconds(MapStrikeStartDelay);
        private static readonly WaitForSeconds MapStrikeIntervalWait = new WaitForSeconds(MapStrikeInterval);

        private readonly Dictionary<UnitRuntimeState, RaidBossLightningVfxPool.Entry> pendingTargetStrikes = new Dictionary<UnitRuntimeState, RaidBossLightningVfxPool.Entry>(16);
        private readonly List<Vector3> mapStrikePositions = new List<Vector3>(32);
        private readonly List<Vector3> candidatePositions = new List<Vector3>(256);
        private RaidBattleController battle;
        private RaidBoardRuntime boardRuntime;
        private Transform runtimeRoot;
        private RaidBossLightningVfxPool vfxPool;
        private RaidBossLightningAudioPool audioPool;
        private RaidBossLightningImpactFeedback impactFeedback;
        private Coroutine mapBarrageRoutine;
        private Coroutine releaseClockRoutine;
        private int strikeColorOffset;

        public static RaidBossLightningRuntime EnsureInstalled(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            RaidBossLightningRuntime runtime = host.GetComponent<RaidBossLightningRuntime>();
            if (runtime == null)
            {
                runtime = host.AddComponent<RaidBossLightningRuntime>();
            }

            return runtime;
        }

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            boardRuntime = GetComponent<RaidBoardRuntime>();
            EnsureRuntimeRoot();
            vfxPool = new RaidBossLightningVfxPool(runtimeRoot);
            audioPool = new RaidBossLightningAudioPool(runtimeRoot);
            impactFeedback = RaidBossLightningImpactFeedback.EnsureInstalled(runtimeRoot.gameObject);
        }

        private void OnEnable()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (boardRuntime == null)
            {
                boardRuntime = GetComponent<RaidBoardRuntime>();
            }

            if (battle == null)
            {
                return;
            }

            battle.OnBossSkillCastStarted += HandleBossSkillCastStarted;
            battle.OnBossSkillUnitStrikeStarted += HandleBossSkillUnitStrikeStarted;
            battle.OnBossSkillUnitStruck += HandleBossSkillUnitStruck;
            battle.OnRaidEnded += HandleRaidEnded;
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnBossSkillCastStarted -= HandleBossSkillCastStarted;
                battle.OnBossSkillUnitStrikeStarted -= HandleBossSkillUnitStrikeStarted;
                battle.OnBossSkillUnitStruck -= HandleBossSkillUnitStruck;
                battle.OnRaidEnded -= HandleRaidEnded;
            }

            StopAllCoroutines();
            mapBarrageRoutine = null;
            releaseClockRoutine = null;
            StopAllEffects();
        }

        private void OnDestroy()
        {
            vfxPool?.Dispose();
        }

        private void EnsureRuntimeRoot()
        {
            Transform existing = transform.Find("RaidBossLightning");
            if (existing != null)
            {
                runtimeRoot = existing;
                return;
            }

            GameObject root = new GameObject("RaidBossLightning");
            runtimeRoot = root.transform;
            runtimeRoot.SetParent(transform, false);
        }

        private void HandleBossSkillCastStarted()
        {
            StopMapBarrage();
            ReleasePendingTargetStrikes();

            if (vfxPool == null || !vfxPool.IsReady)
            {
                return;
            }

            strikeColorOffset = vfxPool.VariantCount > 1 ? UnityEngine.Random.Range(0, vfxPool.VariantCount) : 0;
            RaidBoard board = boardRuntime != null ? boardRuntime.Board : null;
            RaidBossLightningPattern.Build(board, battle != null ? battle.Config : null, MapStrikeCount, StrikeOffsetY, candidatePositions, mapStrikePositions);

            if (mapStrikePositions.Count > 0)
            {
                mapBarrageRoutine = StartCoroutine(RunMapBarrage());
            }
        }

        private void HandleBossSkillUnitStrikeStarted(UnitRuntimeState target, int strikeIndex, int strikeCount)
        {
            if (target == null || vfxPool == null || !vfxPool.IsReady)
            {
                return;
            }

            if (pendingTargetStrikes.TryGetValue(target, out RaidBossLightningVfxPool.Entry previous))
            {
                vfxPool.ReleaseTravel(previous);
                pendingTargetStrikes.Remove(target);
            }

            Vector3 position = target.transform.position + Vector3.up * StrikeOffsetY;
            StartCoroutine(PlayTargetTravel(target, position, GetVariantIndex(strikeIndex)));
        }

        private void HandleBossSkillUnitStruck(UnitRuntimeState target, int strikeIndex, int strikeCount)
        {
            if (target == null || vfxPool == null || !vfxPool.IsReady)
            {
                return;
            }

            Vector3 position = target.transform.position + Vector3.up * StrikeOffsetY;
            if (pendingTargetStrikes.TryGetValue(target, out RaidBossLightningVfxPool.Entry entry))
            {
                pendingTargetStrikes.Remove(target);
                vfxPool.PromoteToImpact(entry, TargetTravelScale, TargetImpactScale, ImpactSimulationSpeed, ImpactLifetime, Time.unscaledTime);
                EnsureReleaseClock();
            }
            else
            {
                StartCoroutine(PlayFallbackTargetStrike(position, GetVariantIndex(strikeIndex)));
            }

            impactFeedback?.PlayTargetImpact(strikeIndex, strikeCount);
            audioPool?.PlayTarget(position, strikeIndex, strikeCount);
        }

        private IEnumerator RunMapBarrage()
        {
            yield return MapStrikeStartWait;

            int count = mapStrikePositions.Count;
            for (int i = 0; i < count; i++)
            {
                if (!CanContinueStrikeSequence())
                {
                    break;
                }

                StartCoroutine(PlayMapStrikeSequence(mapStrikePositions[i], i, count));

                if (i < count - 1)
                {
                    yield return MapStrikeIntervalWait;
                }
            }

            mapBarrageRoutine = null;
        }

        private IEnumerator PlayMapStrikeSequence(Vector3 position, int strikeIndex, int strikeCount)
        {
            int variantIndex = GetVariantIndex(strikeIndex);
            RaidBossLightningVfxPool.Entry entry = vfxPool.BeginTravel(position, MapTravelScale, variantIndex, TravelSimulationSpeed, MapSkyHeight, MapBoltWidth);
            if (entry == null)
            {
                yield break;
            }

            yield return RevealStrike(entry);

            if (!CanContinueStrikeSequence())
            {
                vfxPool.ReleaseTravel(entry);
                yield break;
            }

            vfxPool.PromoteToImpact(entry, MapTravelScale, MapImpactScale, ImpactSimulationSpeed, ImpactLifetime, Time.unscaledTime);
            EnsureReleaseClock();
            impactFeedback?.PlayMapImpact(strikeIndex, strikeCount);

            if (strikeIndex == 0)
            {
                audioPool?.PlayMap(position, true, false);
            }
            else if (strikeIndex == strikeCount - 1)
            {
                audioPool?.PlayMap(position, false, true);
            }
            else if (strikeIndex % MapSoundStep == 0)
            {
                audioPool?.PlayMap(position, false, false);
            }
        }

        private IEnumerator PlayTargetTravel(UnitRuntimeState target, Vector3 position, int variantIndex)
        {
            RaidBossLightningVfxPool.Entry entry = vfxPool.BeginTravel(position, TargetTravelScale, variantIndex, TravelSimulationSpeed, TargetSkyHeight, TargetBoltWidth);
            if (entry == null)
            {
                yield break;
            }

            pendingTargetStrikes[target] = entry;
            yield return RevealStrike(entry);
        }

        private IEnumerator PlayFallbackTargetStrike(Vector3 position, int variantIndex)
        {
            RaidBossLightningVfxPool.Entry entry = vfxPool.BeginTravel(position, TargetTravelScale, variantIndex, TravelSimulationSpeed, TargetSkyHeight, TargetBoltWidth);
            if (entry == null)
            {
                yield break;
            }

            yield return RevealStrike(entry);
            vfxPool.PromoteToImpact(entry, TargetTravelScale, TargetImpactScale, ImpactSimulationSpeed, ImpactLifetime, Time.unscaledTime);
            EnsureReleaseClock();
        }

        private IEnumerator RevealStrike(RaidBossLightningVfxPool.Entry entry)
        {
            float duration = GetTravelDuration();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!CanContinueStrikeSequence())
                {
                    vfxPool.ReleaseTravel(entry);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = duration > 0.0001f ? Mathf.Clamp01(elapsed / duration) : 1f;
                float reveal = 1f - Mathf.Pow(1f - t, 2.2f);
                vfxPool.SetReveal(entry, reveal);
                vfxPool.SetBoltReveal(entry, reveal);
                yield return null;
            }

            vfxPool.SetReveal(entry, 1f);
            vfxPool.SetBoltReveal(entry, 1f);
        }

        private void EnsureReleaseClock()
        {
            if (releaseClockRoutine == null)
            {
                releaseClockRoutine = StartCoroutine(RunReleaseClock());
            }
        }

        private IEnumerator RunReleaseClock()
        {
            while (vfxPool != null && vfxPool.HasTimedEntries)
            {
                vfxPool.ReleaseExpired(Time.unscaledTime);
                yield return null;
            }

            releaseClockRoutine = null;
        }

        private float GetTravelDuration()
        {
            return battle != null && battle.Config != null ? Mathf.Max(0.05f, battle.Config.BossSkillStrikeTelegraphDuration) : 0.16f;
        }

        private int GetVariantIndex(int sequenceIndex)
        {
            int count = vfxPool != null ? vfxPool.VariantCount : 0;
            if (count <= 1)
            {
                return 0;
            }

            return (strikeColorOffset + Mathf.Max(0, sequenceIndex)) % count;
        }

        private bool CanContinueStrikeSequence()
        {
            return battle != null && battle.State != RaidBattleState.Idle && battle.State != RaidBattleState.Victory && battle.State != RaidBattleState.Defeat;
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            StopAllCoroutines();
            mapBarrageRoutine = null;
            releaseClockRoutine = null;
            StopAllEffects();
        }

        private void StopMapBarrage()
        {
            if (mapBarrageRoutine != null)
            {
                StopCoroutine(mapBarrageRoutine);
                mapBarrageRoutine = null;
            }

            mapStrikePositions.Clear();
            candidatePositions.Clear();
        }

        private void ReleasePendingTargetStrikes()
        {
            foreach (RaidBossLightningVfxPool.Entry entry in pendingTargetStrikes.Values)
            {
                vfxPool?.ReleaseTravel(entry);
            }

            pendingTargetStrikes.Clear();
        }

        private void StopAllEffects()
        {
            ReleasePendingTargetStrikes();
            vfxPool?.StopAll();
            audioPool?.StopAll();
            impactFeedback?.StopImmediate();
        }
    }
}
