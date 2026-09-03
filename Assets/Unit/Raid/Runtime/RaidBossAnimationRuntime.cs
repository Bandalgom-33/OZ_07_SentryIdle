using System;
using System.Collections;
using UnityEngine;

namespace EndlessGuard.Unit.Raid.Runtime
{
    /// <summary>
    /// 레이드 보스의 전투 판정과 애니메이션 표현을 분리해서 연결합니다.
    /// - 보스 스킬 게이지 100%로 캐스팅이 시작되면 Charged Spell Cast 재생
    /// - 보스 HP가 실제로 감소하면 Electrocution Reaction 재생
    /// - 원본 레이드 전투 판정/피해/게이지 로직은 변경하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaidBattleController))]
    public sealed class RaidBossAnimationRuntime : MonoBehaviour
    {
        private const string RaidBossRootName = "RaidBoss";
        private const string BossGuideName = "BossGuide";

        private const string IdleStatePath = "Base Layer.Idle";
        private const string BossSkillStatePath = "Base Layer.BossSkill";
        private const string HitStatePath = "Base Layer.Hit";

        private const string IdleClipMarker = "Idle_03";
        private const string BossSkillClipMarker = "Charged_Spell_Cast";
        private const string HitClipMarker = "Electrocution_Reaction";

        private const float DefaultBossSkillClipLength = 2.6667f;
        private const float DefaultHitClipLength = 4.6667f;
        private const float HitPlaybackSpeed = 2.25f;
        private const float HitRetriggerRatio = 0.82f;
        private const float CrossFadeDuration = 0.08f;
        private const float BossHpEpsilon = 0.001f;

        private static readonly int IdleStateHash = Animator.StringToHash(IdleStatePath);
        private static readonly int BossSkillStateHash = Animator.StringToHash(BossSkillStatePath);
        private static readonly int HitStateHash = Animator.StringToHash(HitStatePath);

        private RaidBattleController battle;
        private Animator animator;
        private Animator sourceAnimator;
        private RuntimeAnimatorController bossController;
        private Coroutine oneShotRoutine;
        private float lastBossHp;
        private float nextHitAllowedTime;
        private bool bossSkillAnimationActive;
        private float bossSkillClipLength = DefaultBossSkillClipLength;
        private float hitClipLength = DefaultHitClipLength;
        private bool missingBossWarningLogged;
        private bool missingAnimatorWarningLogged;

        public static RaidBossAnimationRuntime EnsureInstalled(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            RaidBossAnimationRuntime runtime = host.GetComponent<RaidBossAnimationRuntime>();
            if (runtime == null)
            {
                runtime = host.AddComponent<RaidBossAnimationRuntime>();
            }

            return runtime;
        }

        private void Awake()
        {
            battle = GetComponent<RaidBattleController>();
            ResolveAnimator();
        }

        private void OnEnable()
        {
            if (battle == null)
            {
                battle = GetComponent<RaidBattleController>();
            }

            if (battle == null)
            {
                return;
            }

            battle.OnRaidStarted += HandleRaidStarted;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnBossSkillCastStarted += HandleBossSkillCastStarted;
            battle.OnBossSkillCastResolved += HandleBossSkillCastResolved;
            battle.OnBossHpChanged += HandleBossHpChanged;

            lastBossHp = battle.CurrentBossHp;
        }

        private void Start()
        {
            if (animator == null)
            {
                ResolveAnimator();
            }

            PlayIdleImmediate();
        }

        private void Update()
        {
            // Idle_03가 Import 설정상 Loop가 아니어도 보스가 마지막 프레임에서 굳지 않도록 반복합니다.
            if (animator == null || oneShotRoutine != null || bossSkillAnimationActive)
            {
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash == IdleStateHash && stateInfo.normalizedTime >= 0.98f)
            {
                animator.Play(IdleStateHash, 0, 0f);
            }
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidStarted -= HandleRaidStarted;
                battle.OnRaidEnded -= HandleRaidEnded;
                battle.OnBossSkillCastStarted -= HandleBossSkillCastStarted;
                battle.OnBossSkillCastResolved -= HandleBossSkillCastResolved;
                battle.OnBossHpChanged -= HandleBossHpChanged;
            }

            CancelOneShot();
        }

        private void HandleRaidStarted()
        {
            lastBossHp = battle != null ? battle.CurrentBossHp : 0f;
            nextHitAllowedTime = 0f;
            bossSkillAnimationActive = false;
            PlayIdleImmediate();
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            lastBossHp = battle != null ? battle.CurrentBossHp : lastBossHp;
            bossSkillAnimationActive = false;
            CancelOneShot();

            // 죽음 애니메이션은 이번 작업 범위가 아니므로 전투 종료 후 기본 대기 자세로 복귀합니다.
            PlayIdleImmediate();
        }

        private void HandleBossSkillCastStarted()
        {
            if (!EnsureAnimatorReady())
            {
                return;
            }

            bossSkillAnimationActive = true;
            CancelOneShot();

            float castDuration = bossSkillClipLength;
            if (battle != null && battle.Config != null && battle.Config.BossSkillCastDelay > 0.01f)
            {
                castDuration = battle.Config.BossSkillCastDelay;
            }

            // Charged Spell Cast의 전체 모션을 실제 BossSkillCastDelay 안에 맞춰 재생합니다.
            float playbackSpeed = bossSkillClipLength / Mathf.Max(0.05f, castDuration);
            animator.speed = Mathf.Max(0.05f, playbackSpeed);
            animator.CrossFadeInFixedTime(BossSkillStateHash, CrossFadeDuration, 0, 0f);
            oneShotRoutine = StartCoroutine(ReturnToIdleAfter(castDuration, true));
        }

        private void HandleBossSkillCastResolved(int hitCount, float totalDamage)
        {
            bossSkillAnimationActive = false;

            // 타겟이 없어서 스킬이 즉시 끝나는 등의 예외 상황에서도 Idle 상태를 보장합니다.
            if (oneShotRoutine == null)
            {
                PlayIdleImmediate();
            }
        }

        private void HandleBossHpChanged(float currentHp, float maxHp)
        {
            float previousHp = lastBossHp;
            lastBossHp = currentHp;

            if (currentHp <= 0f || currentHp >= previousHp - BossHpEpsilon)
            {
                return;
            }

            TryPlayHitReaction();
        }

        private void TryPlayHitReaction()
        {
            if (!EnsureAnimatorReady() || bossSkillAnimationActive || (battle != null && battle.IsBossSkillCasting))
            {
                return;
            }

            if (Time.time < nextHitAllowedTime)
            {
                return;
            }

            CancelOneShot();

            float hitDuration = hitClipLength / HitPlaybackSpeed;
            nextHitAllowedTime = Time.time + hitDuration * HitRetriggerRatio;

            animator.speed = HitPlaybackSpeed;
            animator.CrossFadeInFixedTime(HitStateHash, CrossFadeDuration, 0, 0f);
            oneShotRoutine = StartCoroutine(ReturnToIdleAfter(hitDuration, false));
        }

        private IEnumerator ReturnToIdleAfter(float duration, bool bossSkill)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!isActiveAndEnabled)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            oneShotRoutine = null;

            if (bossSkill)
            {
                bossSkillAnimationActive = false;
            }

            PlayIdleImmediate();
        }

        private void PlayIdleImmediate()
        {
            if (!EnsureAnimatorReady())
            {
                return;
            }

            CancelOneShot(false);
            animator.speed = 1f;
            animator.Play(IdleStateHash, 0, 0f);
            animator.Update(0f);
        }

        private void CancelOneShot(bool resetAnimatorSpeed = true)
        {
            if (oneShotRoutine != null)
            {
                StopCoroutine(oneShotRoutine);
                oneShotRoutine = null;
            }

            if (resetAnimatorSpeed && animator != null)
            {
                animator.speed = 1f;
            }
        }

        private bool EnsureAnimatorReady()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                ResolveAnimator();
            }

            return animator != null &&
                   animator.runtimeAnimatorController != null &&
                   animator.HasState(0, IdleStateHash) &&
                   animator.HasState(0, BossSkillStateHash) &&
                   animator.HasState(0, HitStateHash);
        }

        private void ResolveAnimator()
        {
            Transform raidBossRoot = FindRaidBossRoot();
            if (raidBossRoot == null)
            {
                LogMissingBossOnce();
                return;
            }

            missingBossWarningLogged = false;
            sourceAnimator = raidBossRoot.GetComponent<Animator>();
            if (sourceAnimator != null && sourceAnimator.runtimeAnimatorController != null)
            {
                bossController = sourceAnimator.runtimeAnimatorController;
            }

            Transform bossGuide = raidBossRoot.Find(BossGuideName);
            Transform visualRoot = FindBossVisualRoot(bossGuide);

            if (visualRoot == null)
            {
                animator = sourceAnimator;
            }
            else
            {
                animator = visualRoot.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = visualRoot.gameObject.AddComponent<Animator>();
                }

                if (bossController != null)
                {
                    animator.runtimeAnimatorController = bossController;
                }

                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;

                // RaidBoss의 기존 Animator는 실제 FBX 리그보다 위에 있으므로
                // Controller를 Visual Root Animator로 넘기고 중복 평가는 막습니다.
                if (sourceAnimator != null && sourceAnimator != animator)
                {
                    sourceAnimator.enabled = false;
                }
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                LogMissingAnimatorOnce();
                return;
            }

            missingAnimatorWarningLogged = false;
            animator.Rebind();
            animator.Update(0f);
            CacheClipLengths();
        }

        private Transform FindRaidBossRoot()
        {
            // RaidBattleController는 씬에서 RaidBattle/Runtime에 있습니다.
            // 따라서 이 컴포넌트의 자식이 아니라 부모 RaidBattle의 형제 구조에서 보스를 찾아야 합니다.
            Transform raidRoot = battle != null ? battle.transform.parent : transform.parent;

            if (raidRoot != null)
            {
                Transform directBoss = raidRoot.Find(RaidBossRootName);
                if (IsRaidBossRoot(directBoss))
                {
                    return directBoss;
                }
            }

            // Hierarchy가 한 단계 바뀌어도 동작하도록 현재 루트 아래를 비활성 오브젝트까지 탐색합니다.
            Transform searchRoot = raidRoot != null ? raidRoot : transform.root;
            if (searchRoot == null)
            {
                return null;
            }

            Transform[] transforms = searchRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null &&
                    string.Equals(candidate.name, RaidBossRootName, StringComparison.Ordinal) &&
                    IsRaidBossRoot(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsRaidBossRoot(Transform candidate)
        {
            return candidate != null && candidate.Find(BossGuideName) != null;
        }

        private static Transform FindBossVisualRoot(Transform bossGuide)
        {
            if (bossGuide == null)
            {
                return null;
            }

            // FBX Root에 원래 Animator가 있으면 그 Root를 최우선으로 사용합니다.
            Animator[] childAnimators = bossGuide.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < childAnimators.Length; i++)
            {
                Animator childAnimator = childAnimators[i];
                if (childAnimator != null &&
                    childAnimator.transform != bossGuide &&
                    childAnimator.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                {
                    return childAnimator.transform;
                }
            }

            for (int i = 0; i < bossGuide.childCount; i++)
            {
                Transform child = bossGuide.GetChild(i);
                if (child != null && child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                {
                    return child;
                }
            }

            return bossGuide.childCount > 0 ? bossGuide.GetChild(0) : null;
        }

        private void LogMissingBossOnce()
        {
            if (missingBossWarningLogged)
            {
                return;
            }

            missingBossWarningLogged = true;
            Debug.LogWarning("[RaidBossAnimationRuntime] RaidBoss/BossGuide를 찾지 못했습니다. RaidBattle Hierarchy를 확인하세요.", this);
        }

        private void LogMissingAnimatorOnce()
        {
            if (missingAnimatorWarningLogged)
            {
                return;
            }

            missingAnimatorWarningLogged = true;
            Debug.LogWarning("[RaidBossAnimationRuntime] 보스 Visual Animator 또는 RaidBoss.controller를 찾지 못했습니다.", this);
        }

        private void CacheClipLengths()
        {
            bossSkillClipLength = ResolveClipLength(BossSkillClipMarker, DefaultBossSkillClipLength);
            hitClipLength = ResolveClipLength(HitClipMarker, DefaultHitClipLength);
        }

        private float ResolveClipLength(string marker, float fallback)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return fallback;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && clip.name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return Mathf.Max(0.05f, clip.length);
                }
            }

            return fallback;
        }
    }
}
