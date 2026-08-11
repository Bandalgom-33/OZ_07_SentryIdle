using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(DefenseBuffSO))]
    [CanEditMultipleObjects]
    public sealed class DefenseBuffSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty trigger;
        private SerializedProperty physicalDefenseBonusPercent;
        private SerializedProperty magicalDefenseBonusPercent;
        private SerializedProperty durationSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();

            trigger = serializedObject.FindProperty("trigger");
            physicalDefenseBonusPercent = serializedObject.FindProperty("physicalDefenseBonusPercent");
            magicalDefenseBonusPercent = serializedObject.FindProperty("magicalDefenseBonusPercent");
            durationSeconds = serializedObject.FindProperty("durationSeconds");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("방어력 증가 조건", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(trigger, new GUIContent("발동 조건", "물리·마법 방어력 증가 효과가 활성화되는 조건입니다."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("방어력 증가 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(physicalDefenseBonusPercent, new GUIContent("물리 방어력 증가율 (%)", "새 캐릭터에 처음 복사할 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
            EditorGUILayout.PropertyField(magicalDefenseBonusPercent, new GUIContent("마법 방어력 증가율 (%)", "새 캐릭터에 처음 복사할 추천값입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));

            if (trigger.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("여러 패시브의 발동 조건이 서로 달라 지속시간 설정을 함께 표시하지 않습니다.", MessageType.Info);
                return;
            }

            DefenseBuffTrigger selectedTrigger = (DefenseBuffTrigger)trigger.intValue;

            if (selectedTrigger == DefenseBuffTrigger.EvadeSuccess)
            {
                EditorGUILayout.PropertyField(durationSeconds, new GUIContent("지속시간 (초)", "회피 성공 후 방어력 증가 효과가 유지되는 시간입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
                return;
            }

            if (selectedTrigger == DefenseBuffTrigger.BlockingSmall ||
                selectedTrigger == DefenseBuffTrigger.BlockingMedium ||
                selectedTrigger == DefenseBuffTrigger.BlockingLarge)
            {
                EditorGUILayout.HelpBox("저지 조건 패시브는 해당 크기의 몬스터를 저지하고 있는 동안 방어력 증가 효과가 유지되므로 별도의 지속시간을 사용하지 않습니다.", MessageType.Info);
            }
        }
    }
}