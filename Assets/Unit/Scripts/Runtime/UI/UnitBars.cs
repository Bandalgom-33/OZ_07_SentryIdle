using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class UnitBars : MonoBehaviour
    {
        [Header("캐릭터 상태 바")]
        [Tooltip("현재 HP 비율에 따라 가로 길이가 변경되는 HP 채움 영역입니다.")]
        [SerializeField] private RectTransform hpFill;

        [Tooltip("현재 스킬 게이지 비율에 따라 가로 길이가 변경되는 스킬 게이지 채움 영역입니다.")]
        [SerializeField] private RectTransform skillFill;

        [Header("HP 표시")]
        [Tooltip("마지막 피해 또는 레이드 회복 아이템 표시 후 HP 바를 유지하는 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float hpVisibleDuration = 2f;

        [Tooltip("HP 바가 사라질 때 사용하는 페이드 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float hpFadeDuration = 0.25f;

        [Tooltip("회복 시 현재 HP 위치에서 회복 후 HP 위치까지 채워지는 시간입니다.")]
        [Min(0f)]
        [SerializeField] private float healFillDuration = 0.3f;

        private static Camera cachedMainCamera;

        private UnitRuntimeState unit;
        private RectTransform hpRoot;
        private RectTransform skillRoot;
        private Graphic[] hpGraphics;
        private Coroutine hpVisibilityRoutine;
        private Coroutine hpHealRoutine;
        private float lastHpActivityTime;
        private float healFillStart;
        private float healFillTarget;
        private float healFillElapsed;
        private bool skillReady;
        private bool subscribed;

        public UnitRuntimeState Unit => unit;
        public float HealthFill => unit != null && unit.Health != null ? unit.Health.NormalizedHp : 0f;
        public float SkillFill => unit != null ? unit.NormalizedSkillGauge : 0f;

        private void Awake()
        {
            FindUnit();
            CacheVisuals();
        }

        private void OnEnable()
        {
            FindUnit();
            CacheVisuals();
            RefreshFacing();
            ResetHpVisibility();
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopHpVisibilityRoutine();
            StopHealFillRoutine(false);

            if (unit != null)
            {
                ReadyEffect.Hide(unit);
            }

            skillReady = false;
        }

        public void RefreshAll()
        {
            FindUnit();

            if (unit == null)
            {
                return;
            }

            RefreshHealth();
            RefreshSkillGauge();
        }

        public void RefreshFacing()
        {
            if (cachedMainCamera == null)
            {
                cachedMainCamera = Camera.main;
            }

            if (cachedMainCamera == null)
            {
                return;
            }

            transform.rotation = cachedMainCamera.transform.rotation;
        }

        private void CacheVisuals()
        {
            if (hpRoot == null && hpFill != null)
            {
                hpRoot = hpFill.parent as RectTransform;
            }

            if (skillRoot == null && skillFill != null)
            {
                skillRoot = skillFill.parent as RectTransform;
            }

            if ((hpGraphics == null || hpGraphics.Length == 0) && hpRoot != null)
            {
                hpGraphics = hpRoot.GetComponentsInChildren<Graphic>(true);
            }
        }

        private void FindUnit()
        {
            if (unit == null)
            {
                unit = GetComponentInParent<UnitRuntimeState>();
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            FindUnit();

            if (unit == null || unit.Health == null)
            {
                return;
            }

            unit.Health.OnHealthChanged += HandleHealthChanged;
            unit.Health.OnDamaged += HandleDamaged;
            unit.Health.OnDied += HandleDied;
            unit.OnSkillGaugeChanged += HandleSkillGaugeChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (unit != null)
            {
                if (unit.Health != null)
                {
                    unit.Health.OnHealthChanged -= HandleHealthChanged;
                    unit.Health.OnDamaged -= HandleDamaged;
                    unit.Health.OnDied -= HandleDied;
                }

                unit.OnSkillGaugeChanged -= HandleSkillGaugeChanged;
            }

            subscribed = false;
        }

        private void HandleHealthChanged(CombatHealth health)
        {
            if (health == null)
            {
                return;
            }

            SetFill(hpFill, health.NormalizedHp);
        }

        private void HandleDamaged(CombatHealth health, float appliedDamage)
        {
            if (health == null || appliedDamage <= 0f || health.IsDead)
            {
                return;
            }

            StopHealFillRoutine(false);
            ShowHpBar();
        }

        public void PlayHealItemFeedback(float healedAmount)
        {
            FindUnit();
            CacheVisuals();

            if (unit == null || unit.Health == null || healedAmount <= 0f || unit.Health.IsDead)
            {
                return;
            }

            float targetNormalized = unit.Health.NormalizedHp;
            float previousNormalized = unit.Health.MaxHp > 0f ? Mathf.Clamp01((unit.Health.CurrentHp - healedAmount) / unit.Health.MaxHp) : targetNormalized;

            StopHealFillRoutine(false);
            healFillStart = previousNormalized;
            healFillTarget = targetNormalized;
            healFillElapsed = 0f;
            SetFill(hpFill, healFillStart);
            ShowHpBar();

            if (healFillDuration <= 0f || hpFill == null || healFillTarget <= healFillStart)
            {
                SetFill(hpFill, healFillTarget);
                return;
            }

            hpHealRoutine = StartCoroutine(HealFillRoutine());
        }

        private void HandleDied(CombatHealth health)
        {
            StopHpVisibilityRoutine();
            StopHealFillRoutine(false);
            SetHpAlpha(0f);
            SetRootActive(hpRoot, false);
            SetRootActive(skillRoot, false);

            if (unit != null)
            {
                ReadyEffect.Hide(unit);
            }

            skillReady = false;
        }

        private void HandleSkillGaugeChanged(UnitRuntimeState changedUnit)
        {
            if (changedUnit != unit)
            {
                return;
            }

            RefreshSkillGauge();
        }

        private void RefreshHealth()
        {
            float normalized = unit.Health != null ? unit.Health.NormalizedHp : 0f;
            SetFill(hpFill, normalized);
        }

        private void RefreshSkillGauge()
        {
            SetFill(skillFill, unit.NormalizedSkillGauge);
            RefreshSkillVisibility();
        }

        private void RefreshSkillVisibility()
        {
            if (unit == null || unit.MaxSkillGauge <= 0f)
            {
                SetRootActive(skillRoot, false);

                if (skillReady)
                {
                    ReadyEffect.Hide(unit);
                }

                skillReady = false;
                return;
            }

            bool readyNow = unit.IsSkillReady;

            if (readyNow)
            {
                SetRootActive(skillRoot, false);

                if (!skillReady)
                {
                    ReadyEffect.Show(unit);
                }
            }
            else
            {
                SetRootActive(skillRoot, true);

                if (skillReady)
                {
                    ReadyEffect.Hide(unit);
                }
            }

            skillReady = readyNow;
        }

        private void ShowHpBar()
        {
            lastHpActivityTime = Time.unscaledTime;
            SetRootActive(hpRoot, true);
            SetHpAlpha(1f);

            if (hpVisibilityRoutine == null)
            {
                hpVisibilityRoutine = StartCoroutine(HpVisibilityRoutine());
            }
        }

        private IEnumerator HealFillRoutine()
        {
            while (true)
            {
                if (healFillDuration <= 0f || hpFill == null || healFillTarget <= healFillStart)
                {
                    SetFill(hpFill, healFillTarget);
                    hpHealRoutine = null;
                    yield break;
                }

                healFillElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(healFillElapsed / healFillDuration);
                float value = Mathf.SmoothStep(healFillStart, healFillTarget, progress);
                SetFill(hpFill, value);

                if (progress >= 1f)
                {
                    SetFill(hpFill, healFillTarget);
                    hpHealRoutine = null;
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator HpVisibilityRoutine()
        {
            while (true)
            {
                while (Time.unscaledTime - lastHpActivityTime < hpVisibleDuration)
                {
                    yield return null;
                }

                if (hpFadeDuration <= 0f)
                {
                    SetHpAlpha(0f);
                    SetRootActive(hpRoot, false);
                    hpVisibilityRoutine = null;
                    yield break;
                }

                float elapsed = 0f;

                while (elapsed < hpFadeDuration)
                {
                    if (Time.unscaledTime - lastHpActivityTime < hpVisibleDuration)
                    {
                        SetHpAlpha(1f);
                        break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    SetHpAlpha(1f - Mathf.Clamp01(elapsed / hpFadeDuration));
                    yield return null;
                }

                if (Time.unscaledTime - lastHpActivityTime >= hpVisibleDuration)
                {
                    SetHpAlpha(0f);
                    SetRootActive(hpRoot, false);
                    hpVisibilityRoutine = null;
                    yield break;
                }
            }
        }

        private void StopHpVisibilityRoutine()
        {
            if (hpVisibilityRoutine == null)
            {
                return;
            }

            StopCoroutine(hpVisibilityRoutine);
            hpVisibilityRoutine = null;
        }

        private void StopHealFillRoutine(bool snapToCurrentHealth)
        {
            if (hpHealRoutine != null)
            {
                StopCoroutine(hpHealRoutine);
                hpHealRoutine = null;
            }

            healFillElapsed = 0f;

            if (snapToCurrentHealth && unit != null && unit.Health != null)
            {
                float normalized = unit.Health.NormalizedHp;
                healFillStart = normalized;
                healFillTarget = normalized;
                SetFill(hpFill, normalized);
            }
        }

        private void ResetHpVisibility()
        {
            StopHpVisibilityRoutine();
            StopHealFillRoutine(false);
            SetHpAlpha(0f);
            SetRootActive(hpRoot, false);
        }

        private void SetHpAlpha(float alpha)
        {
            if (hpGraphics == null)
            {
                return;
            }

            float value = Mathf.Clamp01(alpha);

            for (int i = 0; i < hpGraphics.Length; i++)
            {
                Graphic graphic = hpGraphics[i];

                if (graphic != null)
                {
                    graphic.canvasRenderer.SetAlpha(value);
                }
            }
        }

        private static void SetRootActive(RectTransform root, bool active)
        {
            if (root != null && root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        private static void SetFill(RectTransform fill, float normalized)
        {
            if (fill == null)
            {
                return;
            }

            float value = Mathf.Clamp01(normalized);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(value, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }
}
