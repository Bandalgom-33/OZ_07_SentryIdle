using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    internal static class UnitSkillSettingsEditorGUI
    {
        internal static void Draw(SerializedProperty skillSettings, SerializedProperty maxSkillGauge)
        {
            if (skillSettings == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            skillSettings.isExpanded = EditorGUILayout.Foldout(skillSettings.isExpanded, "SP 소모 액티브 스킬", true);
            if (!skillSettings.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            SerializedProperty enabled = skillSettings.FindPropertyRelative("enabled");
            SerializedProperty autoCast = skillSettings.FindPropertyRelative("autoCastWhenReady");
            SerializedProperty cost = skillSettings.FindPropertyRelative("skillGaugeCost");
            SerializedProperty scope = skillSettings.FindPropertyRelative("targetScope");
            SerializedProperty attackTarget = skillSettings.FindPropertyRelative("attackTarget");
            SerializedProperty priority = skillSettings.FindPropertyRelative("targetPriority");
            SerializedProperty areaTileRange = skillSettings.FindPropertyRelative("areaTileRange");
            SerializedProperty areaLimit = skillSettings.FindPropertyRelative("areaTargetLimit");
            SerializedProperty damageType = skillSettings.FindPropertyRelative("damageType");
            SerializedProperty powerSource = skillSettings.FindPropertyRelative("attackPowerSource");
            SerializedProperty powerPercent = skillSettings.FindPropertyRelative("attackPowerPercent");
            SerializedProperty flatDamage = skillSettings.FindPropertyRelative("flatDamage");
            SerializedProperty applyDefense = skillSettings.FindPropertyRelative("applyDefense");
            SerializedProperty applyPassives = skillSettings.FindPropertyRelative("applyPassiveDamageModifiers");
            SerializedProperty canCritical = skillSettings.FindPropertyRelative("canCritical");
            SerializedProperty hitMode = skillSettings.FindPropertyRelative("hitMode");
            SerializedProperty hitCount = skillSettings.FindPropertyRelative("hitCount");
            SerializedProperty hitInterval = skillSettings.FindPropertyRelative("hitIntervalSeconds");
            SerializedProperty multiHitMode = skillSettings.FindPropertyRelative("multiHitDamageMode");
            SerializedProperty castLock = skillSettings.FindPropertyRelative("castLockSeconds");
            SerializedProperty vfxPrefab = skillSettings.FindPropertyRelative("vfxPrefab");
            SerializedProperty vfxSpawnMode = skillSettings.FindPropertyRelative("vfxSpawnMode");
            SerializedProperty vfxOffset = skillSettings.FindPropertyRelative("vfxOffset");
            SerializedProperty vfxScale = skillSettings.FindPropertyRelative("vfxScale");
            SerializedProperty vfxEveryHit = skillSettings.FindPropertyRelative("playVfxEveryHit");

            EditorGUILayout.PropertyField(enabled, new GUIContent("스킬 사용", "끄면 이 캐릭터는 SP가 차도 액티브 스킬을 사용하지 않습니다."));

            if (!enabled.hasMultipleDifferentValues && !enabled.boolValue)
            {
                EditorGUILayout.HelpBox("스킬 사용이 꺼져 있습니다. SP는 기존 규칙대로 쌓이지만 자동 스킬은 발동하지 않습니다.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.PropertyField(autoCast, new GUIContent("SP 준비 시 자동 발동"));
            EditorGUILayout.PropertyField(cost, new GUIContent("스킬 SP 소모량", "0이면 최대 SP 전체를 요구하고 소비합니다."));

            if (maxSkillGauge != null &&
                !maxSkillGauge.hasMultipleDifferentValues &&
                !cost.hasMultipleDifferentValues &&
                cost.floatValue > 0f &&
                cost.floatValue > maxSkillGauge.floatValue + 0.0001f)
            {
                EditorGUILayout.HelpBox("스킬 SP 소모량이 최대 SP보다 큽니다. 이 상태에서는 자동 발동할 수 없습니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("대상과 범위", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(scope, new GUIContent("스킬 범위 방식"));
            EditorGUILayout.PropertyField(attackTarget, new GUIContent("공격 가능 대상"));

            if (!attackTarget.hasMultipleDifferentValues && attackTarget.enumValueIndex == (int)AttackTarget.None)
            {
                EditorGUILayout.HelpBox("공격 가능 대상이 '없음'이면 SP가 차도 공격 대상을 찾을 수 없어 스킬을 사용하지 않습니다.", MessageType.Warning);
            }

            UnitSkillTargetScope selectedScope = (UnitSkillTargetScope)scope.enumValueIndex;
            if (selectedScope != UnitSkillTargetScope.MapWide)
            {
                EditorGUILayout.HelpBox(
                    "단일/범위 SP 스킬은 캐릭터의 기본 공격 타일 범위 안에 있는 적만 발동 대상으로 선택합니다. 맵 전체만 이 사거리 제한을 무시합니다.",
                    MessageType.Info);
                EditorGUILayout.PropertyField(priority, new GUIContent("대표 대상 선택"));
            }

            if (selectedScope == UnitSkillTargetScope.Area)
            {
                SkillAreaTileEditorGUI.Draw(areaTileRange);
                EditorGUILayout.PropertyField(areaLimit, new GUIContent("범위 최대 대상 수", "0이면 선택한 영향 타일 안의 모든 적입니다."));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("피해 계산", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(damageType, new GUIContent("피해 종류"));
            if (!damageType.hasMultipleDifferentValues && damageType.enumValueIndex == (int)DamageType.None)
            {
                EditorGUILayout.HelpBox("피해 종류가 '피해 없음'이면 적 방어력 종류를 결정할 수 없습니다. 공격 스킬은 물리 또는 마법 피해를 권장합니다.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(powerSource, new GUIContent("기준 공격력"));
            EditorGUILayout.PropertyField(powerPercent, new GUIContent("스킬 공격력 계수 (%)"));
            EditorGUILayout.PropertyField(flatDamage, new GUIContent("고정 추가 피해"));
            EditorGUILayout.PropertyField(applyDefense, new GUIContent("적 방어력 적용"));
            EditorGUILayout.PropertyField(applyPassives, new GUIContent("패시브 피해 보정 적용"));
            EditorGUILayout.PropertyField(canCritical, new GUIContent("스킬 치명타 가능"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("타격 방식", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hitMode, new GUIContent("공격 방식"));

            UnitSkillHitMode selectedHitMode = (UnitSkillHitMode)hitMode.enumValueIndex;
            if (selectedHitMode == UnitSkillHitMode.MultiHit)
            {
                EditorGUILayout.PropertyField(hitCount, new GUIContent("타격 횟수"));
                EditorGUILayout.PropertyField(hitInterval, new GUIContent("타격 간격 (초)"));
                EditorGUILayout.PropertyField(multiHitMode, new GUIContent("다단히트 피해 방식"));
            }

            EditorGUILayout.PropertyField(castLock, new GUIContent("스킬 중 기본공격 정지 (초)"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(vfxPrefab, new GUIContent("스킬 VFX Prefab", "나중에 캐릭터별로 이 Prefab만 교체하면 됩니다."));
            EditorGUILayout.PropertyField(vfxSpawnMode, new GUIContent("VFX 출력 위치"));
            EditorGUILayout.PropertyField(vfxOffset, new GUIContent("VFX 위치 보정"));
            EditorGUILayout.PropertyField(vfxScale, new GUIContent("VFX 크기 배율"));

            if (selectedHitMode == UnitSkillHitMode.MultiHit)
            {
                EditorGUILayout.PropertyField(vfxEveryHit, new GUIContent("매 타격 VFX 재생"));
            }

            EditorGUI.indentLevel--;
        }
    }
}
