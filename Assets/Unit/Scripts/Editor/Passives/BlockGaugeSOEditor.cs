using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(BlockGaugeSO))]
    [CanEditMultipleObjects]
    public sealed class BlockGaugeSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty skillGaugeGain;

        protected override void OnEnable()
        {
            base.OnEnable();

            skillGaugeGain = serializedObject.FindProperty("skillGaugeGain");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("저지 스킬게이지 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skillGaugeGain, new GUIContent("스킬게이지 획득량", "새로운 몬스터를 저지하는 데 성공할 때 획득하는 기본 추천 스킬게이지입니다. 실제 수치는 캐릭터 데이터의 패시브 개별 수치에서 조정할 수 있습니다."));
        }
    }
}