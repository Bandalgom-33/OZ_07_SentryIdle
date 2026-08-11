using EndlessGuard.Unit.Prototype.Phase2;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor.Phase2
{
    [CustomEditor(typeof(Phase2EventMonitor))]
    public sealed class Phase2EventMonitorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Phase2EventMonitor monitor = (Phase2EventMonitor)target;

            if (EditorApplication.isPlaying && GUILayout.Button("OnUnitGrowthChanged 공개 연결 Probe"))
            {
                monitor.SendGrowthChangedProbe();
                Repaint();
            }

            if (GUILayout.Button("이벤트 카운트 초기화"))
            {
                monitor.ResetCounts();
                Repaint();
            }
        }
    }
}
