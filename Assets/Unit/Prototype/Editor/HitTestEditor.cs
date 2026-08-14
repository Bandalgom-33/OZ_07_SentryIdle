using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [CustomEditor(typeof(HitTest))]
    public sealed class HitTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("명중률 검증 실행"))
            {
                HitTest hitTest = (HitTest)target;
                hitTest.RunTest();
                EditorUtility.SetDirty(hitTest);
                serializedObject.Update();
                Repaint();
            }
        }
    }
}