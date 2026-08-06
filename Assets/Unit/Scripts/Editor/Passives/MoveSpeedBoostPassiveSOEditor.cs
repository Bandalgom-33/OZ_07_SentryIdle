using EndlessGuard.Unit.Data;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(MoveSpeedBoostPassiveSO))]
    [CanEditMultipleObjects]
    public sealed class MoveSpeedBoostPassiveSOEditor : PassiveDataSOEditor
    {
        private SerializedProperty bonusMoveSpeedPercent;

        protected override void OnEnable()
        {
            base.OnEnable();

            bonusMoveSpeedPercent =
                serializedObject.FindProperty("bonusMoveSpeedPercent");
        }

        protected override void DrawSpecificFields()
        {
            EditorGUILayout.Space(8f);

            EditorGUILayout.PropertyField(
                bonusMoveSpeedPercent,
                new GUIContent(
                    "이동속도 증가 비율 (%)",
                    "50을 입력하면 기준 이동속도의 50%가 추가되어 " +
                    "최종 이동속도가 기준값의 150%가 됩니다."));

            if (!bonusMoveSpeedPercent.hasMultipleDifferentValues &&
                bonusMoveSpeedPercent.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox(
                    "이동속도 증가 비율이 0이므로 현재 패시브 효과가 없습니다.",
                    MessageType.Warning);
            }
        }
    }
}