using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(LostHpAttackSO))]
    [CanEditMultipleObjects]
    public sealed class LostHpAttackSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty physicalAttackPerLostHpPercent;
        private SerializedProperty maxPhysicalAttackBonusPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            physicalAttackPerLostHpPercent = serializedObject.FindProperty("physicalAttackPerLostHpPercent");
            maxPhysicalAttackBonusPercent = serializedObject.FindProperty("maxPhysicalAttackBonusPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("잃은 HP 공격력 증가 기본값", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                physicalAttackPerLostHpPercent,
                new GUIContent(
                    "잃은 HP 1%당 물리 공격력 증가율 (%)",
                    "새 캐릭터에 처음 복사할 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 독립적으로 조정할 수 있습니다."));

            EditorGUILayout.PropertyField(
                maxPhysicalAttackBonusPercent,
                new GUIContent(
                    "최대 물리 공격력 증가율 (%)",
                    "이 패시브로 증가할 수 있는 물리 공격력의 최대 비율입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 독립적으로 조정할 수 있습니다."));
        }
    }
}