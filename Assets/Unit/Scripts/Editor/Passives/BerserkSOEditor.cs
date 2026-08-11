using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(BerserkSO))]
    [CanEditMultipleObjects]
    public sealed class BerserkSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty physicalAttackPerLostHpPercent;
        private SerializedProperty maxPhysicalAttackBonusPercent;
        private SerializedProperty magicalAttackPerLostHpPercent;
        private SerializedProperty maxMagicalAttackBonusPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            physicalAttackPerLostHpPercent = serializedObject.FindProperty("physicalAttackPerLostHpPercent");
            maxPhysicalAttackBonusPercent = serializedObject.FindProperty("maxPhysicalAttackBonusPercent");
            magicalAttackPerLostHpPercent = serializedObject.FindProperty("magicalAttackPerLostHpPercent");
            maxMagicalAttackBonusPercent = serializedObject.FindProperty("maxMagicalAttackBonusPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("광전 물리 공격력 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(physicalAttackPerLostHpPercent, new GUIContent("잃은 HP 1%당 물리 공격력 증가율 (%)", "잃은 HP 1%마다 증가하는 기본 추천 물리 공격력 비율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(maxPhysicalAttackBonusPercent, new GUIContent("최대 물리 공격력 증가율 (%)", "광전 패시브로 증가할 수 있는 기본 추천 물리 공격력 최대치입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("광전 마법 공격력 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(magicalAttackPerLostHpPercent, new GUIContent("잃은 HP 1%당 마법 공격력 증가율 (%)", "잃은 HP 1%마다 증가하는 기본 추천 마법 공격력 비율입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(maxMagicalAttackBonusPercent, new GUIContent("최대 마법 공격력 증가율 (%)", "광전 패시브로 증가할 수 있는 기본 추천 마법 공격력 최대치입니다. 실제 수치는 몬스터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}
