using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [CustomEditor(typeof(DamageRuleTest))]
    public sealed class DamageRuleTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("피해 공식 검증 실행"))
            {
                DamageRuleTest damageRuleTest = (DamageRuleTest)target;
                damageRuleTest.RunTest();
                EditorUtility.SetDirty(damageRuleTest);
                serializedObject.Update();
                Repaint();
            }
        }
    }
}