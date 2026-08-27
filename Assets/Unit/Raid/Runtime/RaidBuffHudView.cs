using EndlessGuard.Unit.Raid.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidBuffHudView : MonoBehaviour
    {
        private static readonly Color InactiveFrame = new Color32(28, 29, 35, 232);
        private static readonly Color InactiveTrack = new Color32(12, 13, 18, 230);
        private static readonly Color InactiveText = new Color32(120, 123, 134, 255);
        private static readonly Color AttackFrame = new Color32(54, 30, 82, 244);
        private static readonly Color AttackFill = new Color32(151, 65, 224, 255);
        private static readonly Color AttackGlow = new Color32(205, 119, 255, 255);
        private static readonly Color SpeedFrame = new Color32(24, 48, 82, 244);
        private static readonly Color SpeedFill = new Color32(42, 151, 255, 255);
        private static readonly Color SpeedGlow = new Color32(102, 193, 255, 255);
        private static readonly Color HealFrame = new Color32(24, 72, 50, 244);
        private static readonly Color HealFill = new Color32(44, 205, 104, 255);
        private static readonly Color HealGlow = new Color32(112, 255, 166, 255);
        private const int HighStackThreshold = 5;
        private const string AttackFrameMaterialPath = "BuffHUD/MAT_BuffHudAttack";
        private const string SpeedFrameMaterialPath = "BuffHUD/MAT_BuffHudAttackSpeed";
        private const string AttackRuneMaterialPath = "BuffHUD/MAT_BuffHudRuneAttack";
        private const string SpeedRuneMaterialPath = "BuffHUD/MAT_BuffHudRuneAttackSpeed";
        private const string HealFrameMaterialPath = "BuffHUD/MAT_BuffHudHeal";
        private const string HealRuneMaterialPath = "BuffHUD/MAT_BuffHudRuneHeal";
        private const float NormalPunchPeak = 1.75f;
        private const float HighStackPunchPeak = 1.95f;
        private const float MaxStackPunchPeak = 2.15f;
        private const float RefreshPunchPeak = 1.30f;
        private const float PunchDip = 0.90f;
        private const float NormalShakePixels = 4.5f;
        private const float HighStackShakePixels = 6.5f;
        private const float MaxStackShakePixels = 8.5f;
        private const float RefreshShakePixels = 3.0f;
        private const float HighStackEntryIgnitionDuration = 0.80f;
        private const float HighStackStepIgnitionDuration = 0.58f;
        private const float MaxStackIgnitionDuration = 1.00f;
        private const float InitializeRetryInterval = 0.10f;
        private const float InitializeTimeout = 5.0f;

        [Header("Runtime")]
        [SerializeField] private RaidBattleController battle;

        [Header("Attack")]
        [SerializeField] private Image attackFrame;
        [SerializeField] private Image attackAccent;
        [SerializeField] private TMP_Text attackLabel;
        [SerializeField] private Image attackTrack;
        [SerializeField] private RectTransform attackFill;
        [SerializeField] private Image attackFillImage;
        [SerializeField] private Image attackFillGlow;
        [SerializeField] private RectTransform attackSweep;
        [SerializeField] private Image attackSweepImage;
        [SerializeField] private TMP_Text attackStackText;
        [SerializeField] private TMP_Text attackTimeText;

        [Header("Attack Speed")]
        [SerializeField] private Image speedFrame;
        [SerializeField] private Image speedAccent;
        [SerializeField] private TMP_Text speedLabel;
        [SerializeField] private Image speedTrack;
        [SerializeField] private RectTransform speedFill;
        [SerializeField] private Image speedFillImage;
        [SerializeField] private Image speedFillGlow;
        [SerializeField] private RectTransform speedSweep;
        [SerializeField] private Image speedSweepImage;
        [SerializeField] private TMP_Text speedStackText;
        [SerializeField] private TMP_Text speedTimeText;

        [Header("Heal")]
        [SerializeField] private Image healFrame;
        [SerializeField] private Image healAccent;
        [SerializeField] private TMP_Text healLabel;
        [SerializeField] private Image healTrack;
        [SerializeField] private RectTransform healFill;
        [SerializeField] private Image healFillImage;
        [SerializeField] private Image healFillGlow;
        [SerializeField] private RectTransform healSweep;
        [SerializeField] private Image healSweepImage;
        [SerializeField] private TMP_Text healStackText;
        [SerializeField] private TMP_Text healTimeText;

        private RaidFieldBuffRuntime buffs;
        private BarView attackBar;
        private BarView attackSpeedBar;
        private BarView healBar;
        private Material attackFrameMaterial;
        private Material speedFrameMaterial;
        private Material healFrameMaterial;
        private Material attackRuneMaterial;
        private Material speedRuneMaterial;
        private Material healRuneMaterial;
        private bool initialized;
        private bool subscribed;
        private bool referencesValidated;
        private bool initializationBlocked;
        private bool initializationFailureLogged;
        private float initializeRetryTimer;
        private float initializeElapsedTime;

        private void Start()
        {
            TryInitialize();
        }

        private void OnEnable()
        {
            if (initialized)
            {
                Subscribe();
                return;
            }

            if (!initializationBlocked)
            {
                initializeRetryTimer = 0f;
                initializeElapsedTime = 0f;
                initializationFailureLogged = false;
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ReleaseRuntimeMaterial(ref attackFrameMaterial, attackFrame);
            ReleaseRuntimeMaterial(ref speedFrameMaterial, speedFrame);
            ReleaseRuntimeMaterial(ref healFrameMaterial, healFrame);
            ReleaseRuntimeMaterial(ref attackRuneMaterial, attackAccent);
            ReleaseRuntimeMaterial(ref speedRuneMaterial, speedAccent);
            ReleaseRuntimeMaterial(ref healRuneMaterial, healAccent);
        }

        private void Update()
        {
            if (!initialized)
            {
                RetryInitialize();
                return;
            }

            if (buffs == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            attackBar.Tick(buffs.GetState(RaidItemType.Attack), deltaTime);
            attackSpeedBar.Tick(buffs.GetState(RaidItemType.AttackSpeed), deltaTime);
            healBar.Tick(buffs.GetState(RaidItemType.Heal), deltaTime);
        }

        private void RetryInitialize()
        {
            if (initializationBlocked)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            initializeElapsedTime += deltaTime;
            initializeRetryTimer -= deltaTime;

            if (initializeRetryTimer > 0f)
            {
                return;
            }

            initializeRetryTimer = InitializeRetryInterval;

            if (TryInitialize())
            {
                return;
            }

            if (!initializationFailureLogged && initializeElapsedTime >= InitializeTimeout)
            {
                initializationFailureLogged = true;
                Debug.LogError($"RaidBuffHudView가 {InitializeTimeout:0.#}초 동안 RaidFieldBuffRuntime 연결을 기다렸지만 찾지 못했습니다. RaidRuntimeInstaller의 씬 로드 설치 상태를 확인하세요.", this);
            }
        }

        private bool TryInitialize()
        {
            if (initialized)
            {
                return true;
            }

            if (initializationBlocked)
            {
                return false;
            }

            if (battle == null)
            {
                Debug.LogError("RaidBuffHudView의 Battle 참조가 연결되지 않았습니다.", this);
                initializationBlocked = true;
                return false;
            }

            if (!referencesValidated)
            {
                if (!ValidateReferences())
                {
                    initializationBlocked = true;
                    return false;
                }

                referencesValidated = true;
            }

            buffs = battle.GetComponent<RaidFieldBuffRuntime>();
            if (buffs == null)
            {
                return false;
            }

            attackFrameMaterial = CreateRuntimeMaterial(AttackFrameMaterialPath, attackFrame);
            speedFrameMaterial = CreateRuntimeMaterial(SpeedFrameMaterialPath, speedFrame);
            healFrameMaterial = CreateRuntimeMaterial(HealFrameMaterialPath, healFrame);
            attackRuneMaterial = CreateRuntimeMaterial(AttackRuneMaterialPath, attackAccent);
            speedRuneMaterial = CreateRuntimeMaterial(SpeedRuneMaterialPath, speedAccent);
            healRuneMaterial = CreateRuntimeMaterial(HealRuneMaterialPath, healAccent);
            attackBar = new BarView(attackFrame, attackAccent, attackLabel, attackTrack, attackFill, attackFillImage, attackFillGlow, attackSweep, attackSweepImage, attackStackText, attackTimeText, AttackFrame, AttackFill, AttackGlow, attackFrameMaterial, attackRuneMaterial);
            attackSpeedBar = new BarView(speedFrame, speedAccent, speedLabel, speedTrack, speedFill, speedFillImage, speedFillGlow, speedSweep, speedSweepImage, speedStackText, speedTimeText, SpeedFrame, SpeedFill, SpeedGlow, speedFrameMaterial, speedRuneMaterial);
            healBar = new BarView(healFrame, healAccent, healLabel, healTrack, healFill, healFillImage, healFillGlow, healSweep, healSweepImage, healStackText, healTimeText, HealFrame, HealFill, HealGlow, healFrameMaterial, healRuneMaterial);
            initialized = true;
            initializeRetryTimer = 0f;
            initializeElapsedTime = 0f;
            initializationFailureLogged = false;
            Subscribe();
            attackBar.ApplyState(buffs.GetState(RaidItemType.Attack), false, false);
            attackSpeedBar.ApplyState(buffs.GetState(RaidItemType.AttackSpeed), false, false);
            healBar.ApplyState(buffs.GetState(RaidItemType.Heal), false, false);
            return true;
        }

        private static Material CreateRuntimeMaterial(string resourcePath, Image target)
        {
            Material source = Resources.Load<Material>(resourcePath);
            if (source == null)
            {
                Debug.LogError($"버프 HUD Material을 찾지 못했습니다: Resources/{resourcePath}");
                return null;
            }

            Material runtimeMaterial = Instantiate(source);
            runtimeMaterial.name = source.name + "_Runtime";
            if (target != null)
            {
                target.material = runtimeMaterial;
            }

            return runtimeMaterial;
        }

        private static void ReleaseRuntimeMaterial(ref Material runtimeMaterial, Image target)
        {
            if (target != null && target.material == runtimeMaterial)
            {
                target.material = null;
            }

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        private bool ValidateReferences()
        {
            bool attackValid = attackFrame != null && attackAccent != null && attackLabel != null && attackTrack != null && attackFill != null && attackFillImage != null && attackFillGlow != null && attackSweep != null && attackSweepImage != null && attackStackText != null && attackTimeText != null;
            bool speedValid = speedFrame != null && speedAccent != null && speedLabel != null && speedTrack != null && speedFill != null && speedFillImage != null && speedFillGlow != null && speedSweep != null && speedSweepImage != null && speedStackText != null && speedTimeText != null;
            bool healValid = healFrame != null && healAccent != null && healLabel != null && healTrack != null && healFill != null && healFillImage != null && healFillGlow != null && healSweep != null && healSweepImage != null && healStackText != null && healTimeText != null;
            if (attackValid && speedValid && healValid)
            {
                return true;
            }

            Debug.LogError("RaidBuffHudView의 Hierarchy UI 참조가 완전히 연결되지 않았습니다.", this);
            return false;
        }

        private void Subscribe()
        {
            if (subscribed || buffs == null)
            {
                return;
            }

            buffs.OnBuffChanged += HandleBuffChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (buffs != null)
            {
                buffs.OnBuffChanged -= HandleBuffChanged;
            }

            subscribed = false;
        }

        private void HandleBuffChanged(RaidFieldBuffChangedInfo info)
        {
            BarView bar;
            switch (info.State.Type)
            {
                case RaidItemType.Attack:
                    bar = attackBar;
                    break;
                case RaidItemType.AttackSpeed:
                    bar = attackSpeedBar;
                    break;
                case RaidItemType.Heal:
                    bar = healBar;
                    break;
                default:
                    bar = null;
                    break;
            }
            if (bar == null)
            {
                return;
            }

            bool stackImpact = info.Kind == RaidFieldBuffChangeKind.Activated || info.Kind == RaidFieldBuffChangeKind.StackIncreased;
            bool refreshImpact = info.Kind == RaidFieldBuffChangeKind.Refreshed;
            bar.ApplyState(info.State, stackImpact, refreshImpact);
        }

        private sealed class BarView
        {
            private readonly Image frame;
            private readonly Image accent;
            private readonly TMP_Text label;
            private readonly Image track;
            private readonly RectTransform fill;
            private readonly Image fillImage;
            private readonly Image fillGlow;
            private readonly RectTransform sweep;
            private readonly Image sweepImage;
            private readonly TMP_Text stackText;
            private readonly RectTransform stackRect;
            private readonly Vector2 stackBasePosition;
            private readonly TMP_Text timeText;
            private readonly Color activeFrame;
            private readonly Color fillColor;
            private readonly Color glowColor;
            private readonly Material frameMaterial;
            private readonly Material runeMaterial;
            private int materialStack = int.MinValue;
            private int materialMaxStack = int.MinValue;
            private float punchRemaining;
            private float punchDuration;
            private float punchPeak = NormalPunchPeak;
            private float punchDip = PunchDip;
            private float shakePixels = NormalShakePixels;
            private float sweepRemaining;
            private float sweepDuration;
            private float flashRemaining;
            private float flashDuration;
            private float ignitionRemaining;
            private float ignitionDuration;
            private int displayedSecond = int.MinValue;
            private int displayedStack = -1;

            public BarView(Image frame, Image accent, TMP_Text label, Image track, RectTransform fill, Image fillImage, Image fillGlow, RectTransform sweep, Image sweepImage, TMP_Text stackText, TMP_Text timeText, Color activeFrame, Color fillColor, Color glowColor, Material frameMaterial, Material runeMaterial)
            {
                this.frame = frame;
                this.accent = accent;
                this.label = label;
                this.track = track;
                this.fill = fill;
                this.fillImage = fillImage;
                this.fillGlow = fillGlow;
                this.sweep = sweep;
                this.sweepImage = sweepImage;
                this.stackText = stackText;
                stackRect = stackText.rectTransform;
                stackBasePosition = stackRect.anchoredPosition;
                this.timeText = timeText;
                this.activeFrame = activeFrame;
                this.fillColor = fillColor;
                this.glowColor = glowColor;
                this.frameMaterial = frameMaterial;
                this.runeMaterial = runeMaterial;
            }

            public void ApplyState(RaidFieldBuffState state, bool stackImpact, bool refreshImpact)
            {
                int previousStack = displayedStack < 0 ? 0 : displayedStack;
                SetStack(state.Stack);
                SetSecond(state.IsActive ? Mathf.CeilToInt(state.RemainingSeconds) : -1);

                if (stackImpact)
                {
                    bool maxStack = state.Stack >= state.MaxStack;
                    bool enteredHighStack = previousStack < HighStackThreshold && state.Stack >= HighStackThreshold;
                    bool highStack = state.Stack >= HighStackThreshold;
                    punchPeak = maxStack ? MaxStackPunchPeak : highStack ? HighStackPunchPeak : NormalPunchPeak;
                    punchDip = PunchDip;
                    punchDuration = maxStack ? 0.58f : enteredHighStack ? 0.50f : highStack ? 0.46f : 0.38f;
                    shakePixels = maxStack ? MaxStackShakePixels : highStack ? HighStackShakePixels : NormalShakePixels;
                    punchRemaining = punchDuration;
                    sweepDuration = maxStack ? 0.62f : enteredHighStack ? 0.56f : highStack ? 0.50f : 0.44f;
                    sweepRemaining = sweepDuration;
                    flashDuration = maxStack ? 0.68f : enteredHighStack ? 0.58f : highStack ? 0.46f : 0.36f;
                    flashRemaining = flashDuration;

                    if (highStack)
                    {
                        ignitionDuration = maxStack ? MaxStackIgnitionDuration : enteredHighStack ? HighStackEntryIgnitionDuration : HighStackStepIgnitionDuration;
                        ignitionRemaining = ignitionDuration;
                    }
                }
                else if (refreshImpact)
                {
                    punchPeak = RefreshPunchPeak;
                    punchDip = 0.98f;
                    punchDuration = 0.28f;
                    shakePixels = RefreshShakePixels;
                    punchRemaining = punchDuration;
                    sweepDuration = 0.40f;
                    sweepRemaining = sweepDuration;
                    flashDuration = state.Stack >= HighStackThreshold ? 0.34f : 0.26f;
                    flashRemaining = flashDuration;
                }

                UpdateVisualState(state);
                UpdateIgnitionMaterial();
            }

            public void Tick(RaidFieldBuffState state, float deltaTime)
            {
                SetStack(state.Stack);
                SetSecond(state.IsActive ? Mathf.CeilToInt(state.RemainingSeconds) : -1);
                UpdateVisualState(state);
                Animate(deltaTime, state);
            }

            private void UpdateVisualState(RaidFieldBuffState state)
            {
                UpdateFrameMaterial(state);
                float normalized = state.IsActive ? state.NormalizedRemaining : 0f;
                fill.anchorMax = new Vector2(normalized, 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;

                float stackRatio = state.IsActive ? state.Stack / (float)Mathf.Max(1, state.MaxStack) : 0f;
                float highStackRatio = state.IsActive && state.Stack >= HighStackThreshold ? Mathf.InverseLerp(HighStackThreshold, Mathf.Max(HighStackThreshold + 1, state.MaxStack), state.Stack) : 0f;
                frame.color = state.IsActive ? activeFrame : InactiveFrame;
                track.color = InactiveTrack;
                fillImage.color = state.IsActive ? fillColor : new Color(fillColor.r, fillColor.g, fillColor.b, 0f);
                float glowAlpha = state.IsActive ? 0.20f + 0.10f * stackRatio + 0.08f * highStackRatio : 0f;
                fillGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha);
                accent.color = state.IsActive && state.Stack >= HighStackThreshold ? Color.white : new Color(1f, 1f, 1f, 0f);
                label.color = state.IsActive ? Color.white : InactiveText;
                stackText.color = state.IsActive ? Color.Lerp(Color.white, glowColor, highStackRatio * 0.18f) : InactiveText;
                timeText.color = state.IsActive ? new Color32(224, 226, 235, 255) : InactiveText;
            }

            private void UpdateFrameMaterial(RaidFieldBuffState state)
            {
                if (frameMaterial == null)
                {
                    return;
                }

                int stack = state.IsActive ? state.Stack : 0;
                if (materialStack != stack)
                {
                    materialStack = stack;
                    frameMaterial.SetFloat("_Stack", stack);
                    if (runeMaterial != null)
                    {
                        runeMaterial.SetFloat("_Stack", stack);
                    }
                }

                if (materialMaxStack != state.MaxStack)
                {
                    materialMaxStack = state.MaxStack;
                    float maxStack = Mathf.Max(1, state.MaxStack);
                    frameMaterial.SetFloat("_MaxStack", maxStack);
                    if (runeMaterial != null)
                    {
                        runeMaterial.SetFloat("_MaxStack", maxStack);
                    }
                }
            }

            private void UpdateIgnitionMaterial()
            {
                if (frameMaterial == null)
                {
                    return;
                }

                float ignition = 0f;
                if (ignitionRemaining > 0f && ignitionDuration > 0f)
                {
                    float t = 1f - ignitionRemaining / ignitionDuration;
                    ignition = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                }

                frameMaterial.SetFloat("_Ignition", ignition);
                if (runeMaterial != null)
                {
                    runeMaterial.SetFloat("_Ignition", ignition);
                }
            }

            private void Animate(float deltaTime, RaidFieldBuffState state)
            {
                if (ignitionRemaining > 0f)
                {
                    ignitionRemaining = Mathf.Max(0f, ignitionRemaining - deltaTime);
                }

                UpdateIgnitionMaterial();

                if (punchRemaining > 0f)
                {
                    punchRemaining = Mathf.Max(0f, punchRemaining - deltaTime);
                    float t = punchDuration > 0f ? 1f - punchRemaining / punchDuration : 1f;
                    float scale;
                    if (t < 0.34f)
                    {
                        scale = Mathf.Lerp(1f, punchPeak, t / 0.34f);
                    }
                    else if (t < 0.68f)
                    {
                        scale = Mathf.Lerp(punchPeak, punchDip, (t - 0.34f) / 0.34f);
                    }
                    else
                    {
                        scale = Mathf.Lerp(punchDip, 1f, (t - 0.68f) / 0.32f);
                    }

                    stackRect.localScale = new Vector3(scale, scale, 1f);
                    float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    float shakeX = Mathf.Sin(t * Mathf.PI * 12f) * shakePixels * envelope;
                    float shakeY = Mathf.Sin(t * Mathf.PI * 19f + 0.7f) * shakePixels * 0.34f * envelope;
                    stackRect.anchoredPosition = stackBasePosition + new Vector2(shakeX, shakeY);
                }
                else
                {
                    stackRect.localScale = Vector3.one;
                    stackRect.anchoredPosition = stackBasePosition;
                }

                if (sweepRemaining > 0f)
                {
                    sweepRemaining = Mathf.Max(0f, sweepRemaining - deltaTime);
                    float t = sweepDuration > 0f ? 1f - sweepRemaining / sweepDuration : 1f;
                    float x = Mathf.Lerp(17f, 179f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                    sweep.anchoredPosition = new Vector2(x, 0f);
                    float alpha = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (state.Stack >= HighStackThreshold ? 0.88f : 0.72f);
                    sweepImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                }
                else
                {
                    sweepImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
                }

                if (flashRemaining > 0f)
                {
                    flashRemaining = Mathf.Max(0f, flashRemaining - deltaTime);
                    float flash = flashDuration > 0f ? flashRemaining / flashDuration : 0f;
                    float strength = state.Stack >= state.MaxStack ? 0.92f : state.Stack >= HighStackThreshold ? 0.80f : 0.70f;
                    frame.color = Color.Lerp(frame.color, new Color(glowColor.r, glowColor.g, glowColor.b, 1f), flash * strength);
                }

            }

            private void SetStack(int stack)
            {
                if (displayedStack == stack)
                {
                    return;
                }

                displayedStack = stack;
                stackText.text = "×" + Mathf.Max(0, stack);
            }

            private void SetSecond(int seconds)
            {
                if (displayedSecond == seconds)
                {
                    return;
                }

                displayedSecond = seconds;
                timeText.text = seconds >= 0 ? Mathf.Clamp(seconds, 0, 99).ToString("00") : "--";
            }
        }
    }
}
