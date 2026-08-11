using EndlessGuard.Unit.Prototype.Phase2;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor.Phase2
{
    [CustomEditor(typeof(ProgressionPrototypeController))]
    public sealed class ProgressionPrototypeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ProgressionPrototypeController controller = (ProgressionPrototypeController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime 결과", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("검증 캐릭터", controller.SpawnedUnit, typeof(Component), true);
                EditorGUILayout.IntField("Progress 이벤트 수신", controller.ProgressEventCount);
                EditorGUILayout.TextField("변경 전", controller.BeforeSnapshot ?? string.Empty);
                EditorGUILayout.TextField("변경 후", controller.AfterSnapshot ?? string.Empty);
                EditorGUILayout.TextArea(controller.LastMessage ?? string.Empty);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("버튼은 Play Mode에서 사용합니다. 여기의 성장 수치는 Prototype 복제 데이터에만 적용됩니다.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("레벨/승급 검증 캐릭터 생성")) Execute(controller.SpawnTestUnit);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1레벨 분량 EXP")) Execute(controller.AddOneLevelExperience);
            if (GUILayout.Button("지정 EXP 지급")) Execute(controller.AddCustomExperience);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("승급 승인 결과 적용")) Execute(controller.ApplyApprovedPromotion);
            if (GUILayout.Button("현재 능력치 다시 읽기")) Execute(controller.RefreshSnapshot);
            if (GUILayout.Button("레벨/승급 Prototype 초기화")) Execute(controller.ResetPrototype);
        }

        private void Execute(System.Action action)
        {
            action?.Invoke();
            Repaint();
        }
    }
}
