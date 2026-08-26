using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Raid.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RaidContributionView : MonoBehaviour
    {
        private const int VisibleRows = 5;

        private readonly GameObject[] rowObjects = new GameObject[VisibleRows];
        private readonly Image[] fills = new Image[VisibleRows];
        private readonly float[] targetFillAmounts = new float[VisibleRows];
        private readonly Text[] rankTexts = new Text[VisibleRows];
        private readonly Text[] nameTexts = new Text[VisibleRows];
        private readonly Text[] damageTexts = new Text[VisibleRows];
        private readonly Text[] percentTexts = new Text[VisibleRows];
        private readonly List<RaidContributionSnapshot> snapshots = new List<RaidContributionSnapshot>(RaidRosterRuntime.TotalSlots);

        private RaidContributionRuntime contribution;
        private bool dirty = true;
        private float refreshElapsed;
        private static Font koreanFont;

        private void Awake()
        {
            contribution = GetComponent<RaidContributionRuntime>();

            if (contribution == null)
            {
                contribution = RaidContributionRuntime.EnsureInstalled(gameObject);
            }

            BindHierarchy();
        }

        private void OnEnable()
        {
            if (contribution != null)
            {
                contribution.OnContributionChanged += HandleChanged;

                if (contribution.TotalDamage <= 0f)
                {
                    HideAllRowsImmediate();
                }
            }

            dirty = true;
        }

        private void OnDisable()
        {
            if (contribution != null)
            {
                contribution.OnContributionChanged -= HandleChanged;
            }
        }

        private void Update()
        {
            AnimateBars();

            if (!dirty || contribution == null)
            {
                return;
            }

            refreshElapsed += Time.unscaledDeltaTime;

            if (refreshElapsed < 0.1f)
            {
                return;
            }

            refreshElapsed = 0f;
            dirty = false;
            Refresh();
        }

        private void BindHierarchy()
        {
            Transform raidRoot = transform.parent;

            if (raidRoot == null)
            {
                return;
            }

            Transform panel = raidRoot.Find("UI/Contribution");

            if (panel == null)
            {
                Debug.LogWarning("Raid Contribution UI Hierarchy를 찾지 못했습니다: RaidBattle/UI/Contribution", this);
                return;
            }

            Font font = GetKoreanFont();
            Text title = FindText(panel, "Title");

            if (title != null)
            {
                ApplyFont(title, font);
                title.text = "기여도";
            }

            for (int i = 0; i < VisibleRows; i++)
            {
                Transform row = panel.Find($"Row{i + 1}");

                if (row == null)
                {
                    continue;
                }

                rowObjects[i] = row.gameObject;
                fills[i] = FindImage(row, "Fill");
                rankTexts[i] = FindText(row, "Rank");
                nameTexts[i] = FindText(row, "Name");
                damageTexts[i] = FindText(row, "Damage");
                percentTexts[i] = FindText(row, "Percent");

                if (fills[i] != null)
                {
                    fills[i].fillAmount = 0f;
                }

                targetFillAmounts[i] = 0f;
                rowObjects[i].SetActive(false);

                ApplyFont(rankTexts[i], font);
                ApplyFont(nameTexts[i], font);
                ApplyFont(damageTexts[i], font);
                ApplyFont(percentTexts[i], font);
            }

            HideAllRowsImmediate();
        }

        private void Refresh()
        {
            contribution.FillSorted(snapshots);
            float topDamage = snapshots.Count > 0 ? Mathf.Max(1f, snapshots[0].Damage) : 1f;

            for (int i = 0; i < VisibleRows; i++)
            {
                bool hasEntry = i < snapshots.Count;

                if (!hasEntry)
                {
                    targetFillAmounts[i] = 0f;

                    if (rowObjects[i] != null)
                    {
                        rowObjects[i].SetActive(false);
                    }

                    if (fills[i] != null)
                    {
                        fills[i].fillAmount = 0f;
                    }

                    SetText(rankTexts[i], string.Empty);
                    SetText(nameTexts[i], string.Empty);
                    SetText(damageTexts[i], string.Empty);
                    SetText(percentTexts[i], string.Empty);
                    continue;
                }

                bool wasHidden = rowObjects[i] != null && !rowObjects[i].activeSelf;

                if (rowObjects[i] != null)
                {
                    rowObjects[i].SetActive(true);
                }

                if (wasHidden && fills[i] != null)
                {
                    fills[i].fillAmount = 0f;
                }

                targetFillAmounts[i] = Mathf.Clamp01(snapshots[i].Damage / topDamage);

                RaidContributionSnapshot entry = snapshots[i];
                string name = string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.UnitId : entry.DisplayName;

                SetText(rankTexts[i], $"{i + 1}");
                SetText(nameTexts[i], name);
                SetText(damageTexts[i], entry.Damage.ToString("#,0"));
                SetText(percentTexts[i], $"{entry.Ratio * 100f:0.0}%");
            }
        }

        private void HideAllRowsImmediate()
        {
            for (int i = 0; i < VisibleRows; i++)
            {
                targetFillAmounts[i] = 0f;

                if (fills[i] != null)
                {
                    fills[i].fillAmount = 0f;
                }

                if (rowObjects[i] != null)
                {
                    rowObjects[i].SetActive(false);
                }

                SetText(rankTexts[i], string.Empty);
                SetText(nameTexts[i], string.Empty);
                SetText(damageTexts[i], string.Empty);
                SetText(percentTexts[i], string.Empty);
            }
        }

        private void AnimateBars()
        {
            const float FillUnitsPerSecond = 2.8f;
            float step = FillUnitsPerSecond * Time.unscaledDeltaTime;

            for (int i = 0; i < fills.Length; i++)
            {
                Image fill = fills[i];

                if (fill == null || rowObjects[i] == null || !rowObjects[i].activeSelf)
                {
                    continue;
                }

                fill.fillAmount = Mathf.MoveTowards(
                    fill.fillAmount,
                    targetFillAmounts[i],
                    step);
            }
        }

        private void HandleChanged()
        {
            dirty = true;
        }

        private static Text FindText(Transform root, string path)
        {
            Transform target = root != null ? root.Find(path) : null;
            return target != null ? target.GetComponent<Text>() : null;
        }

        private static Image FindImage(Transform root, string path)
        {
            Transform target = root != null ? root.Find(path) : null;
            return target != null ? target.GetComponent<Image>() : null;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void ApplyFont(Text target, Font font)
        {
            if (target == null || font == null || target.font != null)
            {
                return;
            }

            target.font = font;
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
                Font font = Font.CreateDynamicFontFromOSFont(candidates[i], 15);

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
