using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(GrowthTest))]
    public sealed class GrowthTestEditor : UnityEditor.Editor
    {
        private SerializedProperty maxHpAmount;
        private SerializedProperty hpRegenAmount;
        private SerializedProperty physicalAttackAmount;
        private SerializedProperty magicalAttackAmount;
        private SerializedProperty physicalDefenseAmount;
        private SerializedProperty magicalDefenseAmount;
        private SerializedProperty attackSpeedAmount;
        private SerializedProperty accuracyAmount;
        private SerializedProperty evasionAmount;
        private SerializedProperty criticalChanceAmount;
        private SerializedProperty criticalDamageAmount;
        private SerializedProperty lastAppliedUnitCount;
        private SerializedProperty lastMessage;

        private void OnEnable()
        {
            maxHpAmount = serializedObject.FindProperty("maxHpAmount");
            hpRegenAmount = serializedObject.FindProperty("hpRegenAmount");
            physicalAttackAmount = serializedObject.FindProperty("physicalAttackAmount");
            magicalAttackAmount = serializedObject.FindProperty("magicalAttackAmount");
            physicalDefenseAmount = serializedObject.FindProperty("physicalDefenseAmount");
            magicalDefenseAmount = serializedObject.FindProperty("magicalDefenseAmount");
            attackSpeedAmount = serializedObject.FindProperty("attackSpeedAmount");
            accuracyAmount = serializedObject.FindProperty("accuracyAmount");
            evasionAmount = serializedObject.FindProperty("evasionAmount");
            criticalChanceAmount = serializedObject.FindProperty("criticalChanceAmount");
            criticalDamageAmount = serializedObject.FindProperty("criticalDamageAmount");
            lastAppliedUnitCount = serializedObject.FindProperty("lastAppliedUnitCount");
            lastMessage = serializedObject.FindProperty("lastMessage");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("골드 공통 성장 검증 수치", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxHpAmount, new GUIContent("최대 HP 증가량"));
            EditorGUILayout.PropertyField(hpRegenAmount, new GUIContent("초당 HP 재생 증가량"));
            EditorGUILayout.PropertyField(physicalAttackAmount, new GUIContent("물리 공격력 증가량"));
            EditorGUILayout.PropertyField(magicalAttackAmount, new GUIContent("마법 공격력 증가량"));
            EditorGUILayout.PropertyField(physicalDefenseAmount, new GUIContent("물리 방어력 증가량"));
            EditorGUILayout.PropertyField(magicalDefenseAmount, new GUIContent("마법 방어력 증가량"));
            EditorGUILayout.PropertyField(attackSpeedAmount, new GUIContent("공격속도 증가량 (회/초)"));
            EditorGUILayout.PropertyField(accuracyAmount, new GUIContent("명중 증가량"));
            EditorGUILayout.PropertyField(evasionAmount, new GUIContent("회피 증가량"));
            EditorGUILayout.PropertyField(criticalChanceAmount, new GUIContent("치명타 확률 증가량 (%p)"));
            EditorGUILayout.PropertyField(criticalDamageAmount, new GUIContent("치명타 피해량 증가량 (%p)"));

            serializedObject.ApplyModifiedProperties();

            GrowthTest growthTest = (GrowthTest)target;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("골드 공통 성장 버튼", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("아래 성장 버튼은 Play 상태에서 사용합니다.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button($"최대 HP +{growthTest.MaxHpAmount:0.###}"))
                {
                    Execute(growthTest.AddMaxHp);
                }

                if (GUILayout.Button($"초당 HP 재생 +{growthTest.HpRegenAmount:0.###}"))
                {
                    Execute(growthTest.AddHpRegen);
                }

                if (GUILayout.Button($"물리 공격력 +{growthTest.PhysicalAttackAmount:0.###}"))
                {
                    Execute(growthTest.AddPhysicalAttack);
                }

                if (GUILayout.Button($"마법 공격력 +{growthTest.MagicalAttackAmount:0.###}"))
                {
                    Execute(growthTest.AddMagicalAttack);
                }

                if (GUILayout.Button($"물리 방어력 +{growthTest.PhysicalDefenseAmount:0.###}"))
                {
                    Execute(growthTest.AddPhysicalDefense);
                }

                if (GUILayout.Button($"마법 방어력 +{growthTest.MagicalDefenseAmount:0.###}"))
                {
                    Execute(growthTest.AddMagicalDefense);
                }

                if (GUILayout.Button($"공격속도 +{growthTest.AttackSpeedAmount:0.###} 회/초"))
                {
                    Execute(growthTest.AddAttackSpeed);
                }

                if (GUILayout.Button($"명중 +{growthTest.AccuracyAmount:0.###}"))
                {
                    Execute(growthTest.AddAccuracy);
                }

                if (GUILayout.Button($"회피 +{growthTest.EvasionAmount:0.###}"))
                {
                    Execute(growthTest.AddEvasion);
                }

                if (GUILayout.Button($"치명타 확률 +{growthTest.CriticalChanceAmount:0.###}%p"))
                {
                    Execute(growthTest.AddCriticalChance);
                }

                if (GUILayout.Button($"치명타 피해량 +{growthTest.CriticalDamageAmount:0.###}%p"))
                {
                    Execute(growthTest.AddCriticalDamage);
                }
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("마지막 검증 결과", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(lastAppliedUnitCount, new GUIContent("적용 캐릭터 수"));
                EditorGUILayout.PropertyField(lastMessage, new GUIContent("결과"));
            }
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}