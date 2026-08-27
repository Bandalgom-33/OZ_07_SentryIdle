using System.Collections.Generic;
using EndlessGuard.Unit.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidDeploymentNoticeView : MonoBehaviour
    {
        private const float FadeInDuration = 0.12f;
        private const float HoldDuration = 3.00f;
        private const float FadeOutDuration = 0.30f;

        private readonly Queue<string> pendingMessages = new Queue<string>(16);

        private RaidDeploymentRuntime deployment;
        private bool deploymentSubscribed;
        private CanvasGroup canvasGroup;
        private Text messageText;
        private NoticePhase phase;
        private float phaseElapsed;
        private static Font koreanFont;

        private enum NoticePhase
        {
            Hidden,
            FadeIn,
            Hold,
            FadeOut
        }

        private void Awake()
        {
            BindHierarchy();
            HideImmediate();
            TryBindDeployment();
        }

        private void OnEnable()
        {
            TryBindDeployment();
        }

        private void OnDisable()
        {
            UnbindDeployment();
            pendingMessages.Clear();
            HideImmediate();
        }

        private void Update()
        {
            if (canvasGroup == null || messageText == null)
            {
                BindHierarchy();

                if (canvasGroup == null || messageText == null)
                {
                    return;
                }
            }

            if (phase == NoticePhase.Hidden)
            {
                TryShowNext();
                return;
            }

            phaseElapsed += Time.unscaledDeltaTime;

            switch (phase)
            {
                case NoticePhase.FadeIn:
                    canvasGroup.alpha = Mathf.Clamp01(phaseElapsed / FadeInDuration);

                    if (phaseElapsed >= FadeInDuration)
                    {
                        canvasGroup.alpha = 1f;
                        SetPhase(NoticePhase.Hold);
                    }
                    break;

                case NoticePhase.Hold:
                    if (phaseElapsed >= HoldDuration)
                    {
                        SetPhase(NoticePhase.FadeOut);
                    }
                    break;

                case NoticePhase.FadeOut:
                    canvasGroup.alpha =
                        1f - Mathf.Clamp01(phaseElapsed / FadeOutDuration);

                    if (phaseElapsed >= FadeOutDuration)
                    {
                        HideImmediate();
                        TryShowNext();
                    }
                    break;
            }
        }

        private void TryBindDeployment()
        {
            if (deploymentSubscribed && deployment != null)
            {
                return;
            }

            RaidDeploymentRuntime candidate = GetComponent<RaidDeploymentRuntime>();

            if (candidate == null)
            {
                Debug.LogError("RaidDeploymentNoticeView가 RaidDeploymentRuntime을 찾지 못했습니다.", this);
                enabled = false;
                return;
            }

            if (deployment != null &&
                deployment != candidate &&
                deploymentSubscribed)
            {
                deployment.OnUnitDeployed -= HandleUnitDeployed;
                deploymentSubscribed = false;
            }

            deployment = candidate;

            if (isActiveAndEnabled && !deploymentSubscribed)
            {
                deployment.OnUnitDeployed += HandleUnitDeployed;
                deploymentSubscribed = true;
            }
        }

        private void UnbindDeployment()
        {
            if (deployment != null && deploymentSubscribed)
            {
                deployment.OnUnitDeployed -= HandleUnitDeployed;
            }

            deploymentSubscribed = false;
        }

        private void BindHierarchy()
        {
            Transform raidRoot = transform.parent;

            if (raidRoot == null)
            {
                Debug.LogWarning(
                    "Raid Deployment Notice가 RaidBattle 루트를 찾지 못했습니다.",
                    this);
                return;
            }

            Transform panel = raidRoot.Find("UI/DeployNotice");

            if (panel == null)
            {
                Debug.LogWarning(
                    "Raid Deployment Notice Hierarchy를 찾지 못했습니다: RaidBattle/UI/DeployNotice",
                    this);
                return;
            }

            canvasGroup = panel.GetComponent<CanvasGroup>();

            Transform message = panel.Find("Message");
            messageText = message != null ? message.GetComponent<Text>() : null;

            if (messageText != null)
            {
                Font font = GetKoreanFont();

                if (font != null && messageText.font == null)
                {
                    messageText.font = font;
                }
            }
        }

        private void HandleUnitDeployed(RaidUnitDeployedInfo info)
        {
            UnitRuntimeState unit = info.Unit;

            if (unit == null ||
                unit.DataLink == null ||
                !unit.DataLink.HasData)
            {
                return;
            }

            string displayName = unit.DataLink.UnitData.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = unit.UnitId;
            }

            pendingMessages.Enqueue(
                $"<color=#9ED7FF><b>{displayName}</b></color> 전장에 참여합니다");

            if (phase == NoticePhase.Hidden)
            {
                TryShowNext();
            }
        }

        private void TryShowNext()
        {
            if (pendingMessages.Count <= 0 ||
                canvasGroup == null ||
                messageText == null)
            {
                return;
            }

            messageText.text = pendingMessages.Dequeue();
            canvasGroup.alpha = 0f;
            SetPhase(NoticePhase.FadeIn);
        }

        private void SetPhase(NoticePhase next)
        {
            phase = next;
            phaseElapsed = 0f;
        }

        private void HideImmediate()
        {
            phase = NoticePhase.Hidden;
            phaseElapsed = 0f;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (messageText != null)
            {
                messageText.text = string.Empty;
            }
        }

        private static Font GetKoreanFont()
        {
            if (koreanFont != null)
            {
                return koreanFont;
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
                Font font = Font.CreateDynamicFontFromOSFont(candidates[i], 18);

                if (font != null)
                {
                    koreanFont = font;
                    return koreanFont;
                }
            }

            return null;
        }
    }
}
