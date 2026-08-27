using TMPro;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidHudView : MonoBehaviour
    {
        private const int BossSkillSegmentCount = 12;

        private static Font koreanRuntimeFont;

        private static readonly Color AutoColor = new Color32(40, 105, 180, 255);
        private static readonly Color ManualColor = new Color32(124, 78, 46, 255);
        private static readonly Color AttackIdleColor = new Color32(66, 69, 79, 255);
        private static readonly Color AttackReadyColor = new Color32(38, 118, 196, 255);
        private static readonly Color TeamSelectedColor = new Color32(48, 100, 160, 255);
        private static readonly Color TeamIdleColor = new Color32(45, 49, 56, 255);
        private static readonly Color RosterBaseColor = new Color32(48, 51, 58, 255);
        private static readonly Color RosterReadyColor = new Color32(46, 101, 158, 205);
        private static readonly Color RosterDeployedColor = new Color32(52, 82, 112, 210);
        private static readonly Color RosterCooldownColor = new Color32(104, 110, 120, 220);
        private static readonly Color RosterEmptyColor = new Color32(34, 36, 41, 255);

        private RaidBattleController battle;
        private TMP_Text bossNameText;
        private TMP_Text phaseText;
        private RectTransform bossHpFill;
        private TMP_Text bossHpText;
        private TMP_Text bossHpPercentText;
        private readonly RectTransform[] bossSkillSegmentFills = new RectTransform[BossSkillSegmentCount];
        private TMP_Text bossSkillText;
        private TMP_Text timerText;
        private RectTransform raidAttackFill;
        private TMP_Text raidAttackText;
        private Button raidAttackButton;
        private Image raidAttackButtonImage;
        private TMP_Text raidAttackButtonText;
        private RaidAttackHoldInput raidAttackHoldInput;
        private Button modeButton;
        private Image modeButtonImage;
        private TMP_Text modeText;
        private TMP_Text costValueText;
        private Button team1Button;
        private Button team2Button;
        private Image team1Image;
        private Image team2Image;
        private RaidRosterRuntime roster;
        private readonly Image[] rosterSlotImages = new Image[RaidRosterRuntime.SlotsPerTeam];
        private readonly Image[] rosterCooldownFills = new Image[RaidRosterRuntime.SlotsPerTeam];
        private readonly Text[] rosterSlotTexts = new Text[RaidRosterRuntime.SlotsPerTeam];
        private float raidAttackButtonRefreshElapsed;
        private int deploymentSelectedTeam = -1;
        private int deploymentSelectedSlot = -1;

        private void OnEnable()
        {
            battle = GetComponent<RaidBattleController>();
            if (battle == null)
            {
                Debug.LogError("RaidHudView가 RaidBattleController와 같은 오브젝트에 있어야 합니다.", this);
                enabled = false;
                return;
            }

            roster = battle.GetComponent<RaidRosterRuntime>();
            if (roster == null)
            {
                Debug.LogError("RaidHudView가 RaidRosterRuntime을 찾지 못했습니다.", this);
                enabled = false;
                return;
            }

            EnsureEventSystem();

            if (!BindHud())
            {
                enabled = false;
                return;
            }

            battle.OnRaidStarted += HandleRaidStarted;
            battle.OnRaidEnded += HandleRaidEnded;
            battle.OnStateChanged += HandleStateChanged;
            battle.OnModeChanged += HandleModeChanged;
            battle.OnBossHpChanged += HandleBossHpChanged;
            battle.OnTimeChanged += HandleTimeChanged;
            battle.OnBossSkillGaugeChanged += HandleBossSkillGaugeChanged;
            battle.OnRaidAttackGaugeChanged += HandleRaidAttackGaugeChanged;
            battle.OnRaidAttackCastStarted += HandleRaidAttackCastStarted;
            battle.OnRaidAttackCastResolved += HandleRaidAttackCastResolved;
            battle.OnCostChanged += HandleCostChanged;
            battle.OnSelectedTeamChanged += HandleSelectedTeamChanged;
            battle.OnPhaseTransitionStarted += HandlePhaseTransitionStarted;
            battle.OnPhaseTransitionCompleted += HandlePhaseTransitionCompleted;

            if (roster != null)
            {
                roster.OnRosterRebuilt += HandleRosterRebuilt;
                roster.OnSlotChanged += HandleRosterSlotChanged;
            }

            raidAttackHoldInput = raidAttackButton.GetComponent<RaidAttackHoldInput>();
            if (raidAttackHoldInput == null)
            {
                raidAttackHoldInput = raidAttackButton.gameObject.AddComponent<RaidAttackHoldInput>();
            }
            raidAttackHoldInput.Bind(battle);

            modeButton.onClick.AddListener(HandleModeClicked);
            team1Button.onClick.AddListener(HandleTeam1Clicked);
            team2Button.onClick.AddListener(HandleTeam2Clicked);

            RefreshAll();
        }

        private void Update()
        {
            if (battle == null)
            {
                raidAttackButtonRefreshElapsed = 0f;
                return;
            }

            raidAttackButtonRefreshElapsed += Time.unscaledDeltaTime;

            if (raidAttackButtonRefreshElapsed < 0.1f)
            {
                return;
            }

            raidAttackButtonRefreshElapsed = 0f;
            RefreshButtons();
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.OnRaidStarted -= HandleRaidStarted;
                battle.OnRaidEnded -= HandleRaidEnded;
                battle.OnStateChanged -= HandleStateChanged;
                battle.OnModeChanged -= HandleModeChanged;
                battle.OnBossHpChanged -= HandleBossHpChanged;
                battle.OnTimeChanged -= HandleTimeChanged;
                battle.OnBossSkillGaugeChanged -= HandleBossSkillGaugeChanged;
                battle.OnRaidAttackGaugeChanged -= HandleRaidAttackGaugeChanged;
                battle.OnRaidAttackCastStarted -= HandleRaidAttackCastStarted;
                battle.OnRaidAttackCastResolved -= HandleRaidAttackCastResolved;
                battle.OnCostChanged -= HandleCostChanged;
                battle.OnSelectedTeamChanged -= HandleSelectedTeamChanged;
                battle.OnPhaseTransitionStarted -= HandlePhaseTransitionStarted;
                battle.OnPhaseTransitionCompleted -= HandlePhaseTransitionCompleted;
            }

            if (roster != null)
            {
                roster.OnRosterRebuilt -= HandleRosterRebuilt;
                roster.OnSlotChanged -= HandleRosterSlotChanged;
            }

            if (modeButton != null)
            {
                modeButton.onClick.RemoveListener(HandleModeClicked);
            }

            if (team1Button != null)
            {
                team1Button.onClick.RemoveListener(HandleTeam1Clicked);
            }

            if (team2Button != null)
            {
                team2Button.onClick.RemoveListener(HandleTeam2Clicked);
            }
        }

        public RectTransform GetRosterSlotRect(int visibleSlotIndex)
        {
            if (visibleSlotIndex < 0 || visibleSlotIndex >= RaidRosterRuntime.SlotsPerTeam)
            {
                return null;
            }

            Image image = rosterSlotImages[visibleSlotIndex];
            return image != null ? image.rectTransform : null;
        }

        public void SetDeploymentSelection(int teamIndex, int slotIndex)
        {
            deploymentSelectedTeam = teamIndex;
            deploymentSelectedSlot = slotIndex;
            RefreshRosterSlots();
        }

        public void ClearDeploymentSelection()
        {
            if (deploymentSelectedTeam < 0 && deploymentSelectedSlot < 0)
            {
                return;
            }

            deploymentSelectedTeam = -1;
            deploymentSelectedSlot = -1;
            RefreshRosterSlots();
        }

        private bool BindHud()
        {
            Transform raidRoot = battle.transform.parent;
            Transform ui = raidRoot != null ? raidRoot.Find("UI") : null;
            Transform bottomBar = ui != null ? ui.Find("BottomBar") : null;

            if (ui == null || bottomBar == null)
            {
                Debug.LogError("RaidHudView가 RaidBattle/UI 또는 BottomBar를 찾지 못했습니다.", this);
                return false;
            }

            bossNameText = FindComponent<TMP_Text>(ui, "TopHUD/BossHUD/BossName");
            phaseText = FindComponent<TMP_Text>(ui, "TopHUD/BossHUD/Phase");
            bossHpFill = FindComponent<RectTransform>(ui, "TopHUD/BossHUD/BossHP/Track/Fill");
            bossHpText = FindComponent<TMP_Text>(ui, "TopHUD/BossHUD/BossHP/Value");
            bossHpPercentText = FindComponent<TMP_Text>(ui, "TopHUD/BossHUD/BossHP/Percent");
            bossSkillText = FindComponent<TMP_Text>(ui, "TopHUD/BossHUD/BossSkill/Value");
            timerText = FindComponent<TMP_Text>(ui, "Timer/Value");
            raidAttackFill = FindComponent<RectTransform>(bottomBar, "RaidAttack/Track/Fill");
            raidAttackText = FindComponent<TMP_Text>(bottomBar, "RaidAttack/Value");
            raidAttackButton = FindComponent<Button>(bottomBar, "RaidAttack/Button");
            raidAttackButtonImage = FindComponent<Image>(bottomBar, "RaidAttack/Button");
            raidAttackButtonText = FindComponent<TMP_Text>(bottomBar, "RaidAttack/Button/Text");
            costValueText = FindComponent<TMP_Text>(bottomBar, "Cost/Value");
            modeButton = FindComponent<Button>(bottomBar, "Mode");
            modeButtonImage = FindComponent<Image>(bottomBar, "Mode");
            modeText = FindComponent<TMP_Text>(bottomBar, "Mode/Text (TMP)");
            team1Button = FindComponent<Button>(bottomBar, "Roster/Team1");
            team2Button = FindComponent<Button>(bottomBar, "Roster/Team2");
            team1Image = FindComponent<Image>(bottomBar, "Roster/Team1");
            team2Image = FindComponent<Image>(bottomBar, "Roster/Team2");

            for (int i = 0; i < RaidRosterRuntime.SlotsPerTeam; i++)
            {
                string slotPath = $"Roster/Units/Slot{i + 1}";
                rosterSlotImages[i] = FindComponent<Image>(bottomBar, slotPath);
                rosterCooldownFills[i] = FindComponent<Image>(bottomBar, $"{slotPath}/Cooldown");
                rosterSlotTexts[i] = FindComponent<Text>(bottomBar, $"{slotPath}/Info");
            }

            bool valid =
                bossNameText != null &&
                phaseText != null &&
                bossHpFill != null &&
                bossHpText != null &&
                bossHpPercentText != null &&
                bossSkillText != null &&
                timerText != null &&
                raidAttackFill != null &&
                raidAttackText != null &&
                raidAttackButton != null &&
                raidAttackButtonImage != null &&
                raidAttackButtonText != null &&
                costValueText != null &&
                modeButton != null &&
                modeButtonImage != null &&
                modeText != null &&
                team1Button != null &&
                team2Button != null &&
                team1Image != null &&
                team2Image != null;

            for (int i = 0; i < BossSkillSegmentCount; i++)
            {
                bossSkillSegmentFills[i] = FindComponent<RectTransform>(
                    ui,
                    $"TopHUD/BossHUD/BossSkill/Segments/Segment{i + 1:00}/Fill");

                valid &= bossSkillSegmentFills[i] != null;
            }

            for (int i = 0; i < RaidRosterRuntime.SlotsPerTeam; i++)
            {
                valid &= rosterSlotImages[i] != null && rosterCooldownFills[i] != null && rosterSlotTexts[i] != null;
            }

            if (valid)
            {
                ApplyRosterLegacyFont();
            }

            if (!valid)
            {
                Debug.LogError(
                    "Raid HUD Hierarchy 연결에 실패했습니다. TopHUD, Timer, BottomBar/RaidAttack, Cost/Mode/Roster 및 Slot1~8의 Info/Cooldown을 확인하세요.",
                    this);
            }

            return valid;
        }

        private void ApplyRosterLegacyFont()
        {
            Font koreanFont = GetKoreanRuntimeFont();

            if (koreanFont == null)
            {
                Debug.LogWarning(
                    "Raid 캐릭터 이름용 한글 시스템 폰트를 찾지 못했습니다. UI 담당이 Slot Info의 Text Font를 한글 지원 폰트로 교체해야 합니다.",
                    this);
                return;
            }

            for (int i = 0; i < rosterSlotTexts.Length; i++)
            {
                Text label = rosterSlotTexts[i];

                if (label == null)
                {
                    continue;
                }

                label.font = koreanFont;
                label.fontSize = 12;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.supportRichText = true;
                label.lineSpacing = 0.9f;
            }
        }

        private static Font GetKoreanRuntimeFont()
        {
            if (koreanRuntimeFont != null)
            {
                return koreanRuntimeFont;
            }

            string[] candidates =
            {
                "Malgun Gothic",
                "맑은 고딕",
                "Noto Sans CJK KR",
                "Noto Sans KR",
                "Apple SD Gothic Neo",
                "NanumGothic"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Font font = Font.CreateDynamicFontFromOSFont(candidates[i], 16);

                if (font != null)
                {
                    koreanRuntimeFont = font;
                    return koreanRuntimeFont;
                }
            }

            return null;
        }

        private static T FindComponent<T>(Transform root, string path) where T : Component
        {
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

        private void HandleRaidStarted()
        {
            RefreshAll();
        }

        private void HandleRaidEnded(RaidBattleResult result)
        {
            RefreshButtons();
        }

        private void HandleStateChanged(RaidBattleState nextState)
        {
            RefreshButtons();
        }

        private void HandleModeChanged(RaidBattleMode nextMode)
        {
            modeText.text = nextMode == RaidBattleMode.Auto ? "Auto" : "Manual";
            modeButtonImage.color = nextMode == RaidBattleMode.Auto ? AutoColor : ManualColor;
            RefreshButtons();
        }

        private void HandleBossHpChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            SetFill(bossHpFill, ratio);
            bossHpText.text = $"{current:N0} / {max:N0}";
            bossHpPercentText.text = $"{ratio * 100f:0.0}%";
        }

        private void HandleTimeChanged(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void HandleBossSkillGaugeChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            SetSegmentedFill(bossSkillSegmentFills, ratio);
            bossSkillText.text = $"{ratio * 100f:0}%";
        }

        private void HandleRaidAttackGaugeChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            SetFill(raidAttackFill, ratio);
            raidAttackText.text = $"{ratio * 100f:0}%  {current:0} / {max:0}";
            RefreshButtons();
        }

        private void HandleRaidAttackCastStarted(int participantCount)
        {
            RefreshButtons();
        }

        private void HandleRaidAttackCastResolved(int participantCount, float appliedDamage)
        {
            RefreshButtons();
        }

        private void HandleCostChanged(int current, int max)
        {
            costValueText.text = current.ToString();
        }

        private void HandleSelectedTeamChanged(int teamIndex)
        {
            team1Image.color = teamIndex == 0 ? TeamSelectedColor : TeamIdleColor;
            team2Image.color = teamIndex == 1 ? TeamSelectedColor : TeamIdleColor;
            RefreshRosterSlots();
        }

        private void HandleRosterRebuilt()
        {
            RefreshRosterSlots();
        }

        private void HandleRosterSlotChanged(RaidRosterSlotState slot)
        {
            if (slot != null && slot.TeamIndex == battle.SelectedTeamIndex)
            {
                RefreshRosterSlot(slot.SlotIndex, slot);
            }
        }

        private void HandlePhaseTransitionStarted(RaidPhaseTransitionInfo info)
        {
            phaseText.text = $"PHASE {(int)info.FromPhase + 1}  >  {(int)info.ToPhase + 1}";
        }

        private void HandlePhaseTransitionCompleted(RaidPhaseTransitionInfo info)
        {
            RefreshPhase();
        }

        private void HandleModeClicked()
        {
            battle.SetMode(battle.Mode == RaidBattleMode.Auto ? RaidBattleMode.Manual : RaidBattleMode.Auto);
        }

        private void HandleTeam1Clicked()
        {
            battle.SetSelectedTeam(0);
        }

        private void HandleTeam2Clicked()
        {
            battle.SetSelectedTeam(1);
        }

        private void RefreshAll()
        {
            bossNameText.text = battle.BossDisplayName;
            RefreshPhase();
            HandleBossHpChanged(battle.CurrentBossHp, battle.BossMaxHp);
            HandleTimeChanged(battle.RemainingTime);
            HandleBossSkillGaugeChanged(battle.BossSkillGauge, battle.BossSkillGaugeMax);
            HandleRaidAttackGaugeChanged(battle.RaidAttackGauge, battle.RaidAttackGaugeMax);
            HandleCostChanged(battle.CurrentCost, battle.CostMax);
            HandleSelectedTeamChanged(battle.SelectedTeamIndex);
            HandleModeChanged(battle.Mode);
        }

        private void RefreshRosterSlots()
        {
            if (roster == null)
            {
                return;
            }

            int teamIndex = battle != null ? battle.SelectedTeamIndex : 0;

            for (int i = 0; i < RaidRosterRuntime.SlotsPerTeam; i++)
            {
                RefreshRosterSlot(i, roster.GetSlot(teamIndex, i));
            }
        }

        private void RefreshRosterSlot(int visualIndex, RaidRosterSlotState slot)
        {
            if (visualIndex < 0 || visualIndex >= RaidRosterRuntime.SlotsPerTeam)
            {
                return;
            }

            Image baseImage = rosterSlotImages[visualIndex];
            Image fillImage = rosterCooldownFills[visualIndex];
            Text infoText = rosterSlotTexts[visualIndex];

            if (baseImage == null || fillImage == null || infoText == null)
            {
                return;
            }

            if (slot == null || !slot.HasUnit)
            {
                baseImage.color = RosterEmptyColor;
                fillImage.fillAmount = 0f;
                infoText.text = "EMPTY";
                return;
            }

            UnitDataSO data = slot.UnitData;
            string displayName = !string.IsNullOrWhiteSpace(data.DisplayName) ? data.DisplayName : data.name;
            string statusLabel;
            float fillAmount;
            Color fillColor;

            switch (slot.Status)
            {
                case RaidRosterSlotStatus.Deployed:
                    statusLabel = "DEPLOYED";
                    fillAmount = 1f;
                    fillColor = RosterDeployedColor;
                    break;
                case RaidRosterSlotStatus.RedeployCooldown:
                    statusLabel = $"REDEPLOY {slot.RedeployRemaining:0.0}s";
                    fillAmount = slot.RedeployReadyRatio;
                    fillColor = RosterReadyColor;
                    break;
                case RaidRosterSlotStatus.Ready:
                    statusLabel = "READY";
                    fillAmount = 1f;
                    fillColor = RosterReadyColor;
                    break;
                default:
                    statusLabel = "EMPTY";
                    fillAmount = 0f;
                    fillColor = RosterEmptyColor;
                    break;
            }

            baseImage.color = slot.Status == RaidRosterSlotStatus.RedeployCooldown ? RosterCooldownColor : RosterBaseColor;

            if (slot.TeamIndex == deploymentSelectedTeam && slot.SlotIndex == deploymentSelectedSlot)
            {
                baseImage.color = new Color32(55, 132, 207, 255);
            }

            fillImage.color = fillColor;
            fillImage.fillAmount = Mathf.Clamp01(fillAmount);
            infoText.text = $"[{visualIndex + 1}] {displayName}  C{data.SummonCost}\n{statusLabel}";
        }

        private void RefreshPhase()
        {
            phaseText.text = $"PHASE {(int)battle.CurrentPhase + 1}";
        }

        private void RefreshButtons()
        {
            modeButton.interactable = !battle.IsTransitioning;

            bool manual = battle.Mode == RaidBattleMode.Manual;
            bool active = battle.IsRaidAttackActive;
            bool hasGauge = battle.RaidAttackGauge > 0.0001f;
            bool hasParticipants = battle.RaidAttackParticipantCount > 0;

            raidAttackButton.interactable = manual && hasGauge && hasParticipants && !battle.IsTransitioning;
            raidAttackButtonImage.color = active || battle.IsRaidAttackReady ? AttackReadyColor : AttackIdleColor;

            if (active)
            {
                raidAttackButtonText.text = manual ? "HOLDING" : "AUTO FIRING";
            }
            else if (!manual)
            {
                raidAttackButtonText.text = battle.IsRaidAttackReady && !hasParticipants ? "WAIT" : "AUTO";
            }
            else
            {
                raidAttackButtonText.text = hasGauge ? "HOLD ATTACK" : "RAID ATTACK";
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void SetFill(RectTransform fill, float ratio)
        {
            Vector2 max = fill.anchorMax;
            max.x = Mathf.Clamp01(ratio);
            fill.anchorMax = max;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        private static void SetSegmentedFill(RectTransform[] fills, float ratio)
        {
            float scaled = Mathf.Clamp01(ratio) * fills.Length;

            for (int i = 0; i < fills.Length; i++)
            {
                SetFill(fills[i], Mathf.Clamp01(scaled - i));
            }
        }
    }
}
