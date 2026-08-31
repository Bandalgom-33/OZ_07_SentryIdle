using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitRuntimeState))]
    public sealed class UnitAnimationBridge : MonoBehaviour
    {
        private const string DefaultIdleState = "IDLE";
        private const string DefaultMoveBool = "1_Move";
        private const string DefaultAttackTrigger = "2_Attack";
        private const string DefaultDamagedTrigger = "3_Damaged";
        private const string DefaultDeathTrigger = "4_Death";
        private const string DefaultBuffTrigger = "6_Other";
        private const string DefaultSummonTrigger = "6_Other";
        private const string DefaultSkillTrigger = "6_Other";
        private const string DefaultDeathBool = "isDeath";

        [Header("Animator")]
        [Tooltip("비워 두면 VisualRoot 아래의 Animator를 자동으로 찾습니다.")]
        [SerializeField] private Animator animator;

        [Tooltip("Animator 참조가 없거나 외형이 교체되었을 때 VisualRoot에서 Animator를 다시 찾습니다.")]
        [SerializeField] private bool autoFindAnimator = true;

        [Header("State")]
        [Tooltip("대기 상태 이름입니다. SPUM 기본값은 IDLE입니다.")]
        [SerializeField] private string idleStateName = DefaultIdleState;

        [Header("Parameters - SPUM Default")]
        [SerializeField] private string moveBoolParameter = DefaultMoveBool;
        [SerializeField] private string attackTriggerParameter = DefaultAttackTrigger;
        [SerializeField] private string damagedTriggerParameter = DefaultDamagedTrigger;
        [SerializeField] private string deathTriggerParameter = DefaultDeathTrigger;

        [Tooltip("버프 행동에 사용하는 Trigger입니다. SPUM 기본 Controller에서는 6_Other를 사용합니다.")]
        [SerializeField] private string buffTriggerParameter = DefaultBuffTrigger;

        [Tooltip("소환 행동에 사용하는 Trigger입니다. SPUM 기본 Controller에서는 6_Other를 사용합니다. 나중에 전용 소환 Trigger가 생기면 Inspector에서 이름만 바꾸면 됩니다.")]
        [SerializeField] private string summonTriggerParameter = DefaultSummonTrigger;

        [Tooltip("SP 소모 액티브 스킬에 사용하는 Trigger입니다. SPUM 기본 Controller에서는 6_Other를 사용하고, 나중에 전용 Skill Trigger가 생기면 이 이름만 바꾸면 됩니다.")]
        [SerializeField] private string skillTriggerParameter = DefaultSkillTrigger;

        [SerializeField] private string deathBoolParameter = DefaultDeathBool;

        [Header("Optional")]
        [Tooltip("활성화하면 피격 시 DAMAGED 애니메이션도 재생합니다. 기본은 기존 피격 피드백과 충돌하지 않도록 꺼져 있습니다.")]
        [SerializeField] private bool playDamagedWhenHit;

        [Header("Death Presentation")]
        [Tooltip("켜면 Animator Controller의 Death 애니메이션 클립 길이를 찾아 사라지는 시간을 자동으로 맞춥니다.")]
        [SerializeField] private bool useDeathClipLength = true;

        [Tooltip("Death 클립을 찾을 때 사용할 이름 힌트입니다. SPUM 기본값은 DEATH입니다.")]
        [SerializeField] private string deathClipNameHint = "DEATH";

        [Tooltip("Death 클립 길이를 찾지 못했을 때 사용할 기본 재생 시간입니다. SPUM 기본 DEATH는 약 0.667초입니다.")]
        [Min(0.05f)]
        [SerializeField] private float deathAnimationFallbackSeconds = 0.7f;

        [Tooltip("Death 애니메이션이 끝난 뒤 마지막 자세를 화면에 유지할 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float deathHoldSeconds = 0.35f;

        private const float MinimumDeathPresentationSeconds = 0.05f;

        private readonly Dictionary<int, AnimatorControllerParameterType> parameterTypes = new Dictionary<int, AnimatorControllerParameterType>(8);

        private UnitRuntimeState state;
        private UnitAttack attack;
        private CombatHealth health;
        private CombatEntityAnchors anchors;
        private RuntimeAnimatorController cachedController;
        private int idleStateHash;
        private int moveBoolHash;
        private int attackTriggerHash;
        private int damagedTriggerHash;
        private int deathTriggerHash;
        private int buffTriggerHash;
        private int summonTriggerHash;
        private int skillTriggerHash;
        private int deathBoolHash;
        private bool subscribed;
        private int lastSupportActionFrame = -1;

        public Animator Animator => animator;
        public bool HasAnimator => EnsureAnimator();
        public float DeathPresentationDuration => ResolveDeathPresentationDuration();

        private void Awake()
        {
            state = GetComponent<UnitRuntimeState>();
            attack = GetComponent<UnitAttack>();
            health = GetComponent<CombatHealth>();
            anchors = GetComponent<CombatEntityAnchors>();
            RebuildHashes();
            EnsureAnimator();
        }

        private void OnEnable()
        {
            Subscribe();
            EnsureAnimator();
            ResetToIdle();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            RebuildHashes();
            cachedController = null;
            parameterTypes.Clear();
        }

        private void OnTransformChildrenChanged()
        {
            if (!autoFindAnimator)
            {
                return;
            }

            if (animator == null || !animator.transform.IsChildOf(transform))
            {
                animator = null;
            }

            EnsureAnimator();
        }

        public void PlayIdle()
        {
            if (!EnsureAnimator())
            {
                return;
            }

            TrySetBool(moveBoolHash, false);
            TrySetBool(deathBoolHash, false);

            if (idleStateHash != 0 && animator.HasState(0, idleStateHash))
            {
                animator.Play(idleStateHash, 0, 0f);
            }
        }

        public void SetMoving(bool isMoving)
        {
            if (!EnsureAnimator())
            {
                return;
            }

            TrySetBool(moveBoolHash, isMoving);
        }

        public void PlayAttack()
        {
            if (!CanPlayAliveAction())
            {
                return;
            }

            TrySetBool(moveBoolHash, false);
            TrySetTrigger(attackTriggerHash);
        }

        public void PlayDamaged()
        {
            if (!CanPlayAliveAction())
            {
                return;
            }

            TrySetTrigger(damagedTriggerHash);
        }

        public void PlayDeath()
        {
            if (!EnsureAnimator())
            {
                return;
            }

            TrySetBool(moveBoolHash, false);
            TrySetBool(deathBoolHash, true);
            TrySetTrigger(deathTriggerHash);
        }

        public void PlayBuff()
        {
            PlayActionTrigger(buffTriggerHash);
        }

        public void PlaySummon()
        {
            PlayActionTrigger(summonTriggerHash);
        }

        public void PlaySkill()
        {
            PlayActionTrigger(skillTriggerHash);
        }

        public void PlaySupportAction()
        {
            PlayBuff();
        }

        private void PlayActionTrigger(int triggerHash)
        {
            if (!CanPlayAliveAction())
            {
                return;
            }

            TrySetBool(moveBoolHash, false);
            TrySetTrigger(triggerHash);
        }

        public void RefreshAnimator()
        {
            animator = null;
            cachedController = null;
            parameterTypes.Clear();
            EnsureAnimator();
        }

        private void ResetToIdle()
        {
            if (!EnsureAnimator())
            {
                return;
            }

            ResetTrigger(attackTriggerHash);
            ResetTrigger(damagedTriggerHash);
            ResetTrigger(deathTriggerHash);
            ResetTrigger(buffTriggerHash);
            if (summonTriggerHash != buffTriggerHash)
            {
                ResetTrigger(summonTriggerHash);
            }
            if (skillTriggerHash != buffTriggerHash && skillTriggerHash != summonTriggerHash)
            {
                ResetTrigger(skillTriggerHash);
            }
            PlayIdle();
        }

        private bool CanPlayAliveAction()
        {
            return (health == null || !health.IsDead) && EnsureAnimator();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (attack != null)
            {
                attack.OnAttackExecuted += HandleAttackExecuted;
            }

            if (health != null)
            {
                health.OnDied += HandleDied;

                if (playDamagedWhenHit)
                {
                    health.OnDamaged += HandleDamaged;
                }
            }

            PassiveRuntimeEvents.OnSummonRequested += HandleSummonRequested;
            UnitAnimationCueEvents.OnRequested += HandleAnimationCueRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (attack != null)
            {
                attack.OnAttackExecuted -= HandleAttackExecuted;
            }

            if (health != null)
            {
                health.OnDied -= HandleDied;
                health.OnDamaged -= HandleDamaged;
            }

            PassiveRuntimeEvents.OnSummonRequested -= HandleSummonRequested;
            UnitAnimationCueEvents.OnRequested -= HandleAnimationCueRequested;
            subscribed = false;
        }

        private void HandleAttackExecuted(UnitAttack sender)
        {
            // A passive support action (buff/summon) can be resolved inside the same
            // basic attack before OnAttackExecuted is raised. In that case the
            // support/cast animation has visual priority over the ordinary attack.
            if (lastSupportActionFrame == Time.frameCount)
            {
                return;
            }

            PlayAttack();
        }

        private void HandleDied(CombatHealth sender)
        {
            PlayDeath();
        }

        private void HandleDamaged(CombatHealth sender, float amount)
        {
            if (playDamagedWhenHit && amount > 0f)
            {
                PlayDamaged();
            }
        }

        private void HandleSummonRequested(PassiveSummonRequest request)
        {
            if (request.UnitOwner == state)
            {
                lastSupportActionFrame = Time.frameCount;
                PlaySummon();
            }
        }

        private void HandleAnimationCueRequested(UnitAnimationCueInfo info)
        {
            if (info.Unit != state)
            {
                return;
            }

            lastSupportActionFrame = Time.frameCount;

            switch (info.Cue)
            {
                case UnitAnimationCue.Buff:
                    PlayBuff();
                    break;
                case UnitAnimationCue.Summon:
                    PlaySummon();
                    break;
                case UnitAnimationCue.Skill:
                    PlaySkill();
                    break;
            }
        }

        private float ResolveDeathPresentationDuration()
        {
            if (!EnsureAnimator())
            {
                return MinimumDeathPresentationSeconds;
            }

            float animationSeconds = Mathf.Max(MinimumDeathPresentationSeconds, deathAnimationFallbackSeconds);

            if (useDeathClipLength && animator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
                float matchedLength = 0f;
                string hint = string.IsNullOrWhiteSpace(deathClipNameHint) ? DefaultDeathStateName : deathClipNameHint.Trim();

                for (int i = 0; i < clips.Length; i++)
                {
                    AnimationClip clip = clips[i];
                    if (clip == null || string.IsNullOrEmpty(clip.name))
                    {
                        continue;
                    }

                    if (string.Equals(clip.name, hint, System.StringComparison.OrdinalIgnoreCase) ||
                        clip.name.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchedLength = Mathf.Max(matchedLength, clip.length);
                    }
                }

                if (matchedLength > 0f)
                {
                    float animatorSpeed = Mathf.Abs(animator.speed);
                    if (animatorSpeed < 0.01f)
                    {
                        animatorSpeed = 1f;
                    }

                    animationSeconds = matchedLength / animatorSpeed;
                }
            }

            return Mathf.Max(MinimumDeathPresentationSeconds, animationSeconds + Mathf.Max(0f, deathHoldSeconds));
        }

        private const string DefaultDeathStateName = "DEATH";

        private bool EnsureAnimator()
        {
            if (animator == null && autoFindAnimator)
            {
                animator = FindBestAnimator();
            }

            if (animator == null)
            {
                cachedController = null;
                parameterTypes.Clear();
                return false;
            }

            if (cachedController != animator.runtimeAnimatorController)
            {
                CacheAnimatorParameters();
            }

            return true;
        }

        private Animator FindBestAnimator()
        {
            Transform searchRoot = anchors != null && anchors.VisualRoot != null ? anchors.VisualRoot : transform;
            Animator[] candidates = searchRoot.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < candidates.Length; i++)
            {
                Animator candidate = candidates[i];
                if (candidate != null && candidate.runtimeAnimatorController != null)
                {
                    return candidate;
                }
            }

            return candidates.Length > 0 ? candidates[0] : null;
        }

        private void CacheAnimatorParameters()
        {
            cachedController = animator != null ? animator.runtimeAnimatorController : null;
            parameterTypes.Clear();

            if (animator == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                parameterTypes[parameter.nameHash] = parameter.type;
            }
        }

        private void RebuildHashes()
        {
            idleStateHash = ToHash(idleStateName);
            moveBoolHash = ToHash(moveBoolParameter);
            attackTriggerHash = ToHash(attackTriggerParameter);
            damagedTriggerHash = ToHash(damagedTriggerParameter);
            deathTriggerHash = ToHash(deathTriggerParameter);
            buffTriggerHash = ToHash(buffTriggerParameter);
            summonTriggerHash = ToHash(summonTriggerParameter);
            skillTriggerHash = ToHash(skillTriggerParameter);
            deathBoolHash = ToHash(deathBoolParameter);
        }

        private void TrySetTrigger(int hash)
        {
            if (hash == 0 || !parameterTypes.TryGetValue(hash, out AnimatorControllerParameterType type) || type != AnimatorControllerParameterType.Trigger)
            {
                return;
            }

            animator.SetTrigger(hash);
        }

        private void ResetTrigger(int hash)
        {
            if (hash == 0 || !parameterTypes.TryGetValue(hash, out AnimatorControllerParameterType type) || type != AnimatorControllerParameterType.Trigger)
            {
                return;
            }

            animator.ResetTrigger(hash);
        }

        private void TrySetBool(int hash, bool value)
        {
            if (hash == 0 || !parameterTypes.TryGetValue(hash, out AnimatorControllerParameterType type) || type != AnimatorControllerParameterType.Bool)
            {
                return;
            }

            animator.SetBool(hash, value);
        }

        private static int ToHash(string parameterName)
        {
            return string.IsNullOrWhiteSpace(parameterName) ? 0 : Animator.StringToHash(parameterName);
        }
    }
}
