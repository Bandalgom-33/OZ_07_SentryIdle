using System.Collections.Generic;
using System.Reflection;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(UnitDataSO))]
    [CanEditMultipleObjects]
    public sealed class UnitDataSOEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<UnitClass, GUIContent[]> SubclassContentCache = new Dictionary<UnitClass, GUIContent[]>();

        private readonly List<PassiveDataSO> allPassiveAssets = new List<PassiveDataSO>();
        private readonly List<PassiveDataSO> compatiblePassiveAssets = new List<PassiveDataSO>();

        private GUIContent[] passiveOptions = { new GUIContent("미설정") };
        private UnitClass cachedPassiveClass = UnitClass.None;
        private string passiveMessage;
        private MessageType passiveMessageType = MessageType.None;

        private SerializedProperty script;
        private SerializedProperty unitId;
        private SerializedProperty displayName;
        private SerializedProperty description;
        private SerializedProperty grade;
        private SerializedProperty initialLevel;
        private SerializedProperty growthTable;
        private SerializedProperty unitClass;
        private SerializedProperty subclass;
        private SerializedProperty placement;
        private SerializedProperty summonCost;
        private SerializedProperty redeployTime;
        private SerializedProperty blockCount;
        private SerializedProperty baseStats;
        private SerializedProperty attackSettings;
        private SerializedProperty hpRegenPerSecond;
        private SerializedProperty criticalChancePercent;
        private SerializedProperty criticalDamageBonusPercent;
        private SerializedProperty maxSkillGauge;
        private SerializedProperty skillGaugeRegenPerSecond;
        private SerializedProperty skillGaugePerAttack;
        private SerializedProperty passives;
        private SerializedProperty passiveTunings;
        private SerializedProperty unitPrefab;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            unitId = serializedObject.FindProperty("unitId");
            displayName = serializedObject.FindProperty("displayName");
            description = serializedObject.FindProperty("description");
            grade = serializedObject.FindProperty("grade");
            initialLevel = serializedObject.FindProperty("initialLevel");
            growthTable = serializedObject.FindProperty("growthTable");
            unitClass = serializedObject.FindProperty("unitClass");
            subclass = serializedObject.FindProperty("subclass");
            placement = serializedObject.FindProperty("placement");
            summonCost = serializedObject.FindProperty("summonCost");
            redeployTime = serializedObject.FindProperty("redeployTime");
            blockCount = serializedObject.FindProperty("blockCount");
            baseStats = serializedObject.FindProperty("baseStats");
            attackSettings = serializedObject.FindProperty("attackSettings");
            hpRegenPerSecond = serializedObject.FindProperty("hpRegenPerSecond");
            criticalChancePercent = serializedObject.FindProperty("criticalChancePercent");
            criticalDamageBonusPercent = serializedObject.FindProperty("criticalDamageBonusPercent");
            maxSkillGauge = serializedObject.FindProperty("maxSkillGauge");
            skillGaugeRegenPerSecond = serializedObject.FindProperty("skillGaugeRegenPerSecond");
            skillGaugePerAttack = serializedObject.FindProperty("skillGaugePerAttack");
            passives = serializedObject.FindProperty("passives");
            passiveTunings = serializedObject.FindProperty("passiveTunings");
            unitPrefab = serializedObject.FindProperty("unitPrefab");

            ReloadPassiveAssets();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CombatDataEditorGUI.DrawReadOnlyProperty(script, "스크립트", "이 데이터 에셋을 정의하는 C# 스크립트입니다.");
            CombatDataEditorGUI.DrawReadOnlyProperty(unitId, "캐릭터 데이터 ID", "제작 도구에서 UNIT_0001 형식으로 자동 발급되며 직접 수정하지 않습니다.");

            if (!unitId.hasMultipleDifferentValues && string.IsNullOrWhiteSpace(unitId.stringValue))
            {
                EditorGUILayout.HelpBox("캐릭터 데이터 ID는 향후 제작 도구에서 자동 발급합니다. 현재 비어 있는 것이 정상입니다.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(displayName, new GUIContent("표시 이름", "게임 화면과 제작 도구에 표시되는 캐릭터 이름입니다."));
            EditorGUILayout.PropertyField(description, new GUIContent("설명", "캐릭터의 역할과 특징을 설명합니다."));
            EditorGUILayout.PropertyField(grade, new GUIContent("성급", "캐릭터의 1성부터 6성까지의 성급입니다."));
            EditorGUILayout.PropertyField(initialLevel, new GUIContent("초기 레벨", "새로운 진행 데이터를 만들 때 이 캐릭터가 시작하는 레벨입니다. 실제 현재 레벨과 경험치는 별도의 진행도 데이터에서 관리합니다."));
            EditorGUILayout.PropertyField(growthTable, new GUIContent("상위 분류 성장 테이블", "캐릭터별 수치를 따로 두지 않고 Vanguard/Guard/Defender 등 상위 분류별 성장 설정을 공유합니다."));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(unitClass, new GUIContent("상위 분류", "캐릭터의 상위 직군입니다. 패시브 선택 가능 풀은 이 상위 분류를 기준으로 결정합니다."));
            bool classChanged = EditorGUI.EndChangeCheck();

            if (classChanged && !unitClass.hasMultipleDifferentValues)
            {
                UnitClass selectedClass = (UnitClass)unitClass.intValue;
                UnitSubclass selectedSubclass = (UnitSubclass)subclass.intValue;

                if (!UnitClassRules.IsSubclassAllowed(selectedClass, selectedSubclass))
                {
                    subclass.intValue = (int)UnitSubclass.None;
                }
            }

            DrawSubclassField();
            RefreshPassiveCandidatesIfNeeded();

            EditorGUILayout.PropertyField(placement, new GUIContent("배치 가능 위치", "지상, 언덕 또는 두 위치 모두에 배치할 수 있는지 설정합니다."));
            EditorGUILayout.PropertyField(summonCost, new GUIContent("소환 코스트", "공통 성장과 패시브가 적용되기 전 기준 소환 코스트입니다."));
            EditorGUILayout.PropertyField(redeployTime, new GUIContent("재배치 시간 (초)", "사망하거나 퇴장한 뒤 다시 소환할 수 있을 때까지의 기준 시간입니다."));
            EditorGUILayout.PropertyField(blockCount, new GUIContent("저지 가능 수", "동시에 이동을 막을 수 있는 지상 몬스터의 최대 수입니다."));

            CombatDataEditorGUI.DrawCombatStats(baseStats);
            CombatDataEditorGUI.DrawAttackSettings(attackSettings);

            EditorGUILayout.PropertyField(hpRegenPerSecond, new GUIContent("초당 HP 재생량", "전투 중 매초 회복하는 기준 HP입니다."));
            EditorGUILayout.PropertyField(criticalChancePercent, new GUIContent("치명타 확률 (%)", "25를 입력하면 25%를 의미합니다."));
            EditorGUILayout.PropertyField(criticalDamageBonusPercent, new GUIContent("치명타 추가 피해 (%)", "50을 입력하면 기본 피해에 50%가 추가됩니다."));

            EditorGUILayout.PropertyField(maxSkillGauge, new GUIContent("최대 스킬게이지", "캐릭터가 보유할 수 있는 최대 스킬게이지입니다."));
            EditorGUILayout.PropertyField(skillGaugeRegenPerSecond, new GUIContent("초당 스킬게이지 회복량", "전투 중 매초 자연 회복하는 스킬게이지입니다."));
            EditorGUILayout.PropertyField(skillGaugePerAttack, new GUIContent("공격당 스킬게이지 획득량", "기본 공격을 한 번 완료할 때 획득하는 스킬게이지입니다."));

            DrawFilteredPassiveList();
            PassiveTuningEditorGUI.Draw(passives, passiveTunings, targets.Length > 1, "캐릭터");

            EditorGUILayout.PropertyField(unitPrefab, new GUIContent("연결 프리팹", "이 데이터를 기준으로 생성되거나 연결된 캐릭터 프리팹입니다."));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSubclassField()
        {
            GUIContent label = new GUIContent("세부 분류", "상위 분류 안에서 캐릭터의 세부 정체성을 설정합니다. 세부 분류는 패시브 선택 가능 여부를 제한하지 않습니다.");

            if (unitClass.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(subclass, label);
                }

                EditorGUILayout.HelpBox("선택한 캐릭터 데이터들의 상위 분류가 서로 달라 세부 분류를 함께 편집할 수 없습니다.", MessageType.Info);
                return;
            }

            UnitClass selectedClass = (UnitClass)unitClass.intValue;
            IReadOnlyList<UnitSubclass> availableSubclasses = UnitClassRules.GetSubclasses(selectedClass);
            UnitSubclass selectedSubclass = (UnitSubclass)subclass.intValue;
            int selectedIndex = FindSubclassIndex(availableSubclasses, selectedSubclass);
            bool hadInvalidSubclass = selectedIndex < 0;

            if (hadInvalidSubclass)
            {
                selectedIndex = 0;
                subclass.intValue = (int)UnitSubclass.None;
            }

            using (new EditorGUI.DisabledScope(selectedClass == UnitClass.None))
            {
                GUIContent[] options = GetSubclassContents(selectedClass);
                int newIndex = EditorGUILayout.Popup(label, selectedIndex, options);

                if (newIndex != selectedIndex)
                {
                    subclass.intValue = (int)availableSubclasses[newIndex];
                }
            }

            if (selectedClass == UnitClass.None)
            {
                EditorGUILayout.HelpBox("상위 분류를 먼저 선택하면 해당 분류의 세부 분류 목록이 표시됩니다.", MessageType.Info);
            }
            else if (hadInvalidSubclass)
            {
                EditorGUILayout.HelpBox("기존 세부 분류가 현재 상위 분류와 맞지 않아 미설정으로 초기화했습니다.", MessageType.Warning);
            }
        }

        private void DrawFilteredPassiveList()
        {
            EditorGUILayout.Space(8f);
            passives.isExpanded = EditorGUILayout.Foldout(passives.isExpanded, "패시브 능력", true);

            if (!passives.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            if (GUILayout.Button("패시브 후보 새로고침"))
            {
                ReloadPassiveAssets();
                passiveMessage = $"패시브 에셋 {allPassiveAssets.Count}개를 다시 검색했습니다.";
                passiveMessageType = MessageType.Info;
            }

            if (unitClass.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("선택한 캐릭터 데이터들의 상위 직군이 서로 달라 패시브를 함께 편집할 수 없습니다.", MessageType.Info);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(passives, new GUIContent("현재 패시브 목록"), true);
                }

                EditorGUI.indentLevel--;
                return;
            }

            UnitClass selectedClass = (UnitClass)unitClass.intValue;

            if (selectedClass == UnitClass.None)
            {
                EditorGUILayout.HelpBox("상위 분류를 먼저 선택하면 해당 직군에서 사용할 수 있는 패시브 후보가 표시됩니다.", MessageType.Info);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(passives, new GUIContent("현재 패시브 목록"), true);
                }

                EditorGUI.indentLevel--;
                return;
            }

            if (selectedClass == UnitClass.Specialist)
            {
                EditorGUILayout.HelpBox("스페셜리스트는 모든 캐릭터용 패시브를 선택할 수 있습니다.", MessageType.Info);
            }

            EditorGUILayout.LabelField($"현재 상위 직군과 호환되는 패시브 후보: {compatiblePassiveAssets.Count}개", EditorStyles.miniLabel);

            int previousSize = passives.arraySize;
            int newSize = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("패시브 개수", "이 캐릭터가 사용하는 패시브의 개수입니다."), previousSize));

            if (newSize != previousSize)
            {
                passives.arraySize = newSize;

                if (newSize > previousSize)
                {
                    for (int i = previousSize; i < newSize; i++)
                    {
                        passives.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    }
                }
            }

            for (int i = 0; i < passives.arraySize; i++)
            {
                SerializedProperty element = passives.GetArrayElementAtIndex(i);
                PassiveDataSO current = element.objectReferenceValue as PassiveDataSO;
                bool isCompatible = current == null || current.CanBeUsedByUnit(selectedClass);
                bool isDuplicate = current != null && PassiveCandidateEditorUtility.IsAlreadyAssigned(passives, current, i);

                if (!isCompatible || isDuplicate)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(new GUIContent($"패시브 {i + 1}"), current, typeof(PassiveDataSO), false);
                    }

                    string reason = !isCompatible
                        ? "현재 캐릭터의 상위 직군 패시브 풀과 호환되지 않는 패시브입니다."
                        : "같은 패시브 에셋이 목록에 중복 등록되어 있습니다.";

                    EditorGUILayout.HelpBox(reason, MessageType.Warning);

                    if (GUILayout.Button($"패시브 {i + 1} 참조 제거"))
                    {
                        element.objectReferenceValue = null;
                    }

                    continue;
                }

                int selectedIndex = PassiveCandidateEditorUtility.FindCandidateIndex(compatiblePassiveAssets, current);
                int newIndex = EditorGUILayout.Popup(new GUIContent($"패시브 {i + 1}"), selectedIndex, passiveOptions);

                if (newIndex != selectedIndex)
                {
                    PassiveDataSO candidate = newIndex == 0 ? null : compatiblePassiveAssets[newIndex - 1];

                    if (PassiveCandidateEditorUtility.IsAlreadyAssigned(passives, candidate, i))
                    {
                        passiveMessage = $"{candidate.DisplayName} 패시브는 이미 이 캐릭터에 등록되어 있습니다.";
                        passiveMessageType = MessageType.Warning;
                    }
                    else
                    {
                        element.objectReferenceValue = candidate;
                        passiveMessage = string.Empty;
                        passiveMessageType = MessageType.None;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(passiveMessage))
            {
                EditorGUILayout.HelpBox(passiveMessage, passiveMessageType);
            }

            EditorGUI.indentLevel--;
        }

        private void ReloadPassiveAssets()
        {
            PassiveCandidateEditorUtility.LoadAllPassives(allPassiveAssets);
            RebuildPassiveCandidates();
        }

        private void RefreshPassiveCandidatesIfNeeded()
        {
            if (unitClass.hasMultipleDifferentValues)
            {
                return;
            }

            UnitClass selectedClass = (UnitClass)unitClass.intValue;

            if (selectedClass != cachedPassiveClass)
            {
                RebuildPassiveCandidates();
            }
        }

        private void RebuildPassiveCandidates()
        {
            UnitClass selectedClass = unitClass != null && !unitClass.hasMultipleDifferentValues
                ? (UnitClass)unitClass.intValue
                : UnitClass.None;

            PassiveCandidateEditorUtility.BuildUnitCandidates(allPassiveAssets, selectedClass, compatiblePassiveAssets);

            passiveOptions = PassiveCandidateEditorUtility.CreateOptionContents(compatiblePassiveAssets);
            cachedPassiveClass = selectedClass;
        }

        private static int FindSubclassIndex(IReadOnlyList<UnitSubclass> subclasses, UnitSubclass target)
        {
            for (int i = 0; i < subclasses.Count; i++)
            {
                if (subclasses[i] == target)
                {
                    return i;
                }
            }

            return -1;
        }

        private static GUIContent[] GetSubclassContents(UnitClass unitClass)
        {
            if (SubclassContentCache.TryGetValue(unitClass, out GUIContent[] cachedContents))
            {
                return cachedContents;
            }

            IReadOnlyList<UnitSubclass> subclasses = UnitClassRules.GetSubclasses(unitClass);
            GUIContent[] contents = new GUIContent[subclasses.Count];

            for (int i = 0; i < subclasses.Count; i++)
            {
                contents[i] = new GUIContent(GetSubclassDisplayName(subclasses[i]));
            }

            SubclassContentCache.Add(unitClass, contents);

            return contents;
        }

        private static string GetSubclassDisplayName(UnitSubclass subclassValue)
        {
            FieldInfo field = typeof(UnitSubclass).GetField(subclassValue.ToString());
            InspectorNameAttribute inspectorName = field?.GetCustomAttribute<InspectorNameAttribute>();

            return inspectorName != null
                ? inspectorName.displayName
                : ObjectNames.NicifyVariableName(subclassValue.ToString());
        }
    }
}