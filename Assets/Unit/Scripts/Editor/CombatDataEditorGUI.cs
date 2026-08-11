using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class CombatDataEditorGUI
    {
        public static void DrawReadOnlyProperty(SerializedProperty property, string label, string tooltip)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            }
        }

        public static void DrawCombatStats(SerializedProperty property)
        {
            EditorGUILayout.Space(8f);
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, "공통 기본 전투 능력치", true);

            if (!property.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(property.FindPropertyRelative("maxHp"), new GUIContent("최대 HP", "레벨 성장, 공통 강화, 패시브와 버프가 적용되기 전 기준 최대 HP입니다."));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("physicalAttack"), new GUIContent("물리 공격력", "물리 기본 공격과 물리 공격형 능력의 기준 공격력입니다."));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("magicalAttack"), new GUIContent("마법 공격력", "마법 기본 공격과 마법 공격형 능력의 기준 공격력입니다."));

            DrawAttackRate(property);

            EditorGUILayout.PropertyField(property.FindPropertyRelative("physicalDefense"), new GUIContent("물리 방어력", "물리 피해를 받을 때 사용하는 기준 방어력입니다."));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("magicalDefense"), new GUIContent("마법 방어력", "마법 피해를 받을 때 사용하는 기준 방어력입니다."));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("accuracy"), new GUIContent("명중력", "최종 명중 확률을 계산할 때 사용하는 기준 능력치이며 그 자체가 퍼센트 값은 아닙니다."));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("evasion"), new GUIContent("회피력", "최종 회피 확률을 계산할 때 사용하는 기준 능력치이며 그 자체가 퍼센트 값은 아닙니다."));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("moveSpeed"), new GUIContent("이동속도", "1초 동안 이동하는 월드 거리의 기준값입니다."));

            EditorGUI.indentLevel--;
        }

        private static void DrawAttackRate(SerializedProperty combatStats)
        {
            SerializedProperty attacksPerSecond = combatStats.FindPropertyRelative("baseAttacksPerSecond");

            if (attacksPerSecond == null)
            {
                EditorGUILayout.HelpBox("기본 공격 빈도 필드를 찾지 못했습니다. CombatStats의 baseAttacksPerSecond 필드를 확인하세요.", MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(attacksPerSecond, new GUIContent("기본 공격 빈도 (회/초)", "강화 전 1초당 기본 공격 횟수입니다. 2는 1초에 2회, 0.5는 2초에 1회를 의미합니다."));

            if (attacksPerSecond.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(new GUIContent("계산된 기본 공격 간격 (초)"), "여러 값");
                    EditorGUILayout.TextField(new GUIContent("기본 공격 해석"), "여러 값");
                }

                return;
            }

            float rate = Mathf.Max(0f, attacksPerSecond.floatValue);

            using (new EditorGUI.DisabledScope(true))
            {
                if (rate <= 0f)
                {
                    EditorGUILayout.TextField(new GUIContent("계산된 기본 공격 간격 (초)"), "공격하지 않음");
                    EditorGUILayout.TextField(new GUIContent("기본 공격 해석"), "기본 공격 빈도가 0입니다.");
                    return;
                }

                float interval = 1f / rate;
                EditorGUILayout.FloatField(new GUIContent("계산된 기본 공격 간격 (초)", "기본 공격 빈도를 기준으로 자동 계산되는 참고값입니다."), interval);
                EditorGUILayout.TextField(new GUIContent("기본 공격 해석", "입력한 공격 빈도를 사람이 읽기 쉬운 문장으로 표시합니다."), BuildAttackRateDescription(rate, interval));
            }
        }

        private static string BuildAttackRateDescription(float attacksPerSecond, float interval)
        {
            if (attacksPerSecond >= 1f)
            {
                return $"1초에 약 {attacksPerSecond:0.###}회 공격";
            }

            return $"약 {interval:0.###}초에 1회 공격";
        }

        public static void DrawAttackSettings(SerializedProperty property)
        {
            EditorGUILayout.Space(8f);
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, "기본 공격 설정", true);

            if (!property.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            SerializedProperty attackMode = property.FindPropertyRelative("attackMode");
            SerializedProperty damageType = property.FindPropertyRelative("damageType");
            SerializedProperty attackTarget = property.FindPropertyRelative("attackTarget");
            SerializedProperty attackRange = property.FindPropertyRelative("attackRange");
            SerializedProperty targetCount = property.FindPropertyRelative("targetCount");
            SerializedProperty basicAttackRange = property.FindPropertyRelative("basicAttackRange");

            EditorGUILayout.PropertyField(attackMode, new GUIContent("공격 방식", "기본 공격을 하지 않는지, 근거리 또는 원거리 방식인지 설정합니다."));
            EditorGUILayout.PropertyField(damageType, new GUIContent("기본 공격 피해 유형", "기본 공격이 물리 피해인지 마법 피해인지 설정합니다."));
            EditorGUILayout.PropertyField(attackTarget, new GUIContent("공격 가능 대상", "지상, 공중 또는 양쪽 모두를 기본 공격할 수 있는지 설정합니다."));
            EditorGUILayout.PropertyField(attackRange, new GUIContent("공격 사거리", "대상을 탐색하고 기본 공격할 수 있는 기준 월드 거리입니다."));
            EditorGUILayout.PropertyField(targetCount, new GUIContent("동시 공격 대상 수", "한 번의 기본 공격으로 동시에 공격할 수 있는 최대 대상 수입니다."));

            DrawRangeRotationMode(property);
            BasicAttackRangeEditorGUI.Draw(basicAttackRange, attackRange);

            EditorGUI.indentLevel--;
        }

        private static void DrawRangeRotationMode(SerializedProperty attackSettings)
        {
            SerializedProperty rotationMode = attackSettings.FindPropertyRelative("rangeRotationMode");

            if (rotationMode == null)
            {
                EditorGUILayout.HelpBox("공격 범위 회전 방식 필드를 찾지 못했습니다. AttackSettings의 rangeRotationMode 필드를 확인하세요.", MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(rotationMode, new GUIContent("공격 범위 회전 방식", "기본 공격 타일 범위는 현재 Facing 방향에 맞춰 사용하며, 전투 중 Facing을 고정할지 유효한 대상 방향에 따라 자동 변경할지 설정합니다."));

            if (rotationMode.hasMultipleDifferentValues)
            {
                return;
            }

            AttackRangeRotationMode selectedMode = (AttackRangeRotationMode)rotationMode.enumValueIndex;

            if (selectedMode == AttackRangeRotationMode.Fixed)
            {
                EditorGUILayout.HelpBox("방향 고정: 초기 Facing을 유지하고, 저장된 +Y 기준 공격 범위를 그 Facing 방향에 맞춰 회전해서 계속 사용합니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("바라보는 방향 따라 회전: 초기 Facing으로 시작하고, 전투 중 유효한 대상 방향에 따라 Facing과 공격 범위가 자동으로 회전합니다.", MessageType.Info);
        }

        public static void DrawPassiveList(SerializedProperty property)
        {
            EditorGUILayout.Space(8f);
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, "패시브 능력", true);

            if (!property.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            int newSize = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("패시브 개수", "이 데이터가 사용하는 패시브 목록의 개수입니다."), property.arraySize));

            if (newSize != property.arraySize)
            {
                property.arraySize = newSize;
            }

            for (int i = 0; i < property.arraySize; i++)
            {
                EditorGUILayout.PropertyField(property.GetArrayElementAtIndex(i), new GUIContent($"패시브 {i + 1}"));
            }

            EditorGUI.indentLevel--;
        }
    }
}