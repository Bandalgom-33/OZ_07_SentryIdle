using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(EnemyDataSO))]
    [CanEditMultipleObjects]
    public sealed class EnemyDataSOEditor : UnityEditor.Editor
    {
        private readonly List<PassiveDataSO> allPassiveAssets = new List<PassiveDataSO>();
        private readonly List<PassiveDataSO> compatiblePassiveAssets = new List<PassiveDataSO>();

        private GUIContent[] passiveOptions = { new GUIContent("미설정") };
        private EnemyCategory cachedCategory = EnemyCategory.None;
        private EnemyMovementType cachedMovementType = EnemyMovementType.None;
        private EnemySize cachedSize = EnemySize.None;
        private EnemyRole cachedRole = EnemyRole.None;
        private string passiveMessage;
        private MessageType passiveMessageType = MessageType.None;

        private SerializedProperty script;
        private SerializedProperty enemyId;
        private SerializedProperty displayName;
        private SerializedProperty description;
        private SerializedProperty category;
        private SerializedProperty movementType;
        private SerializedProperty size;
        private SerializedProperty role;
        private SerializedProperty attackRule;
        private SerializedProperty baseStats;
        private SerializedProperty attackSettings;
        private SerializedProperty rewardExp;
        private SerializedProperty rewardGold;
        private SerializedProperty passives;
        private SerializedProperty enemyPrefab;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            enemyId = serializedObject.FindProperty("enemyId");
            displayName = serializedObject.FindProperty("displayName");
            description = serializedObject.FindProperty("description");
            category = serializedObject.FindProperty("category");
            movementType = serializedObject.FindProperty("movementType");
            size = serializedObject.FindProperty("size");
            role = serializedObject.FindProperty("role");
            attackRule = serializedObject.FindProperty("attackRule");
            baseStats = serializedObject.FindProperty("baseStats");
            attackSettings = serializedObject.FindProperty("attackSettings");
            rewardExp = serializedObject.FindProperty("rewardExp");
            rewardGold = serializedObject.FindProperty("rewardGold");
            passives = serializedObject.FindProperty("passives");
            enemyPrefab = serializedObject.FindProperty("enemyPrefab");

            ReloadPassiveAssets();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CombatDataEditorGUI.DrawReadOnlyProperty(script, "스크립트", "이 데이터 에셋을 정의하는 C# 스크립트입니다.");
            CombatDataEditorGUI.DrawReadOnlyProperty(enemyId, "몬스터 데이터 ID", "제작 도구에서 ENEMY_0001 형식으로 자동 발급되며 직접 수정하지 않습니다.");

            if (!enemyId.hasMultipleDifferentValues && string.IsNullOrWhiteSpace(enemyId.stringValue))
            {
                EditorGUILayout.HelpBox("몬스터 데이터 ID는 향후 제작 도구에서 자동 발급합니다. 현재 비어 있는 것이 정상입니다.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(displayName, new GUIContent("표시 이름", "게임 화면과 제작 도구에 표시되는 몬스터 이름입니다."));
            EditorGUILayout.PropertyField(description, new GUIContent("설명", "몬스터의 특징과 전투 역할을 설명합니다."));

            EditorGUILayout.PropertyField(category, new GUIContent("몬스터 분류", "일반, 엘리트 또는 보스 중 하나를 설정합니다."));
            EditorGUILayout.PropertyField(movementType, new GUIContent("이동 유형", "지상 또는 공중 이동 유형을 설정합니다."));
            EditorGUILayout.PropertyField(size, new GUIContent("몬스터 크기", "패시브와 전투 조건 판정에 사용하는 크기 분류입니다."));
            EditorGUILayout.PropertyField(role, new GUIContent("전투 역할", "공격형 또는 서포터 역할을 설정합니다."));

            DrawAttackRule();
            RefreshPassiveCandidatesIfNeeded();

            CombatDataEditorGUI.DrawCombatStats(baseStats);
            CombatDataEditorGUI.DrawAttackSettings(attackSettings);

            EditorGUILayout.PropertyField(rewardExp, new GUIContent("처치 경험치", "몬스터 사망 시 경험치 담당 시스템이 지급할 기준 경험치입니다."));
            EditorGUILayout.PropertyField(rewardGold, new GUIContent("처치 골드", "몬스터 사망 시 재화 담당 시스템이 지급할 기준 골드입니다."));

            DrawFilteredPassiveList();

            EditorGUILayout.PropertyField(enemyPrefab, new GUIContent("연결 프리팹", "이 데이터를 기준으로 생성되거나 연결된 몬스터 프리팹입니다."));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAttackRule()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.PropertyField(attackRule, new GUIContent("공격 시작 규칙", "저지된 캐릭터만 공격할지, 저지되지 않아도 범위 안 캐릭터를 공격할지 설정합니다."));

            if (attackRule.hasMultipleDifferentValues)
            {
                return;
            }

            EnemyAttackRule selectedRule = (EnemyAttackRule)attackRule.enumValueIndex;

            switch (selectedRule)
            {
                case EnemyAttackRule.BlockedOnly:
                    EditorGUILayout.HelpBox("저지된 대상만 공격: 출구로 이동하다가 캐릭터에게 저지되면 이동을 멈추고 자신을 저지한 캐릭터만 공격합니다.", MessageType.Info);
                    break;

                case EnemyAttackRule.InRange:
                    EditorGUILayout.HelpBox("범위 내 대상 공격: 저지되지 않아도 공격 범위 안의 캐릭터를 찾아 이동을 멈추고 공격합니다. 대상이 사라지면 출구 이동을 재개합니다.", MessageType.Info);
                    break;

                default:
                    EditorGUILayout.HelpBox("몬스터의 공격 시작 규칙이 설정되지 않았습니다.", MessageType.Warning);
                    break;
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

            if (category.hasMultipleDifferentValues || movementType.hasMultipleDifferentValues || size.hasMultipleDifferentValues || role.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("선택한 몬스터 데이터들의 분류 조건이 서로 달라 패시브를 함께 편집할 수 없습니다.", MessageType.Info);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(passives, new GUIContent("현재 패시브 목록"), true);
                }

                EditorGUI.indentLevel--;
                return;
            }

            EnemyCategory selectedCategory = (EnemyCategory)category.intValue;
            EnemyMovementType selectedMovementType = (EnemyMovementType)movementType.intValue;
            EnemySize selectedSize = (EnemySize)size.intValue;
            EnemyRole selectedRole = (EnemyRole)role.intValue;

            if (selectedCategory == EnemyCategory.None || selectedMovementType == EnemyMovementType.None || selectedSize == EnemySize.None || selectedRole == EnemyRole.None)
            {
                EditorGUILayout.HelpBox("몬스터 분류, 이동 유형, 몬스터 크기와 전투 역할을 먼저 모두 선택하면 호환되는 패시브 후보가 표시됩니다.", MessageType.Info);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(passives, new GUIContent("현재 패시브 목록"), true);
                }

                if (passives.arraySize > 0 && GUILayout.Button("현재 패시브 목록 비우기"))
                {
                    passives.ClearArray();
                }

                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.LabelField($"현재 몬스터 분류와 호환되는 패시브 후보: {compatiblePassiveAssets.Count}개", EditorStyles.miniLabel);

            int previousSize = passives.arraySize;
            int newSize = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("패시브 개수", "이 몬스터가 사용하는 패시브의 개수입니다."), previousSize));

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
                bool isCompatible = current == null || current.CanBeUsedByEnemy(selectedCategory, selectedMovementType, selectedSize, selectedRole);
                bool isDuplicate = current != null && PassiveCandidateEditorUtility.IsAlreadyAssigned(passives, current, i);

                if (!isCompatible || isDuplicate)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(new GUIContent($"패시브 {i + 1}"), current, typeof(PassiveDataSO), false);
                    }

                    string reason = !isCompatible ? "현재 몬스터의 분류 조건과 호환되지 않는 패시브입니다." : "같은 패시브 에셋이 목록에 중복 등록되어 있습니다.";
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
                        passiveMessage = $"{candidate.DisplayName} 패시브는 이미 이 몬스터에 등록되어 있습니다.";
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
            if (category.hasMultipleDifferentValues || movementType.hasMultipleDifferentValues || size.hasMultipleDifferentValues || role.hasMultipleDifferentValues)
            {
                return;
            }

            EnemyCategory selectedCategory = (EnemyCategory)category.intValue;
            EnemyMovementType selectedMovementType = (EnemyMovementType)movementType.intValue;
            EnemySize selectedSize = (EnemySize)size.intValue;
            EnemyRole selectedRole = (EnemyRole)role.intValue;

            if (selectedCategory != cachedCategory || selectedMovementType != cachedMovementType || selectedSize != cachedSize || selectedRole != cachedRole)
            {
                RebuildPassiveCandidates();
            }
        }

        private void RebuildPassiveCandidates()
        {
            EnemyCategory selectedCategory = category != null && !category.hasMultipleDifferentValues ? (EnemyCategory)category.intValue : EnemyCategory.None;
            EnemyMovementType selectedMovementType = movementType != null && !movementType.hasMultipleDifferentValues ? (EnemyMovementType)movementType.intValue : EnemyMovementType.None;
            EnemySize selectedSize = size != null && !size.hasMultipleDifferentValues ? (EnemySize)size.intValue : EnemySize.None;
            EnemyRole selectedRole = role != null && !role.hasMultipleDifferentValues ? (EnemyRole)role.intValue : EnemyRole.None;

            PassiveCandidateEditorUtility.BuildEnemyCandidates(allPassiveAssets, selectedCategory, selectedMovementType, selectedSize, selectedRole, compatiblePassiveAssets);
            passiveOptions = PassiveCandidateEditorUtility.CreateOptionContents(compatiblePassiveAssets);
            cachedCategory = selectedCategory;
            cachedMovementType = selectedMovementType;
            cachedSize = selectedSize;
            cachedRole = selectedRole;
        }
    }
}