using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Prototype.Phase2;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor.Phase2
{
    [CustomEditor(typeof(Passive41PrototypeController))]
    public sealed class Passive41PrototypeControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty officialPassives;
        private SerializedProperty selectedPassiveIndex;

        private void OnEnable()
        {
            officialPassives = serializedObject.FindProperty("officialPassives");
            selectedPassiveIndex = serializedObject.FindProperty("selectedPassiveIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "officialPassives", "selectedPassiveIndex",
                "coverageRegisteredCount", "coverageUnsupportedCount", "coverageInvalidCompatibilityCount",
                "activeAssignedPassiveCount", "activeAppliedPassiveCount", "activeRejectedPassiveCount", "activeUnsupportedPassiveCount", "lastMessage");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("정식 패시브 41종", EditorStyles.boldLabel);

            if (GUILayout.Button("정식 패시브 41개 자동 연결"))
            {
                CollectPassives();
            }

            string[] names = BuildPassiveNames();

            if (names.Length > 0)
            {
                selectedPassiveIndex.intValue = EditorGUILayout.Popup("검증 패시브", Mathf.Clamp(selectedPassiveIndex.intValue, 0, names.Length - 1), names);
            }
            else
            {
                EditorGUILayout.HelpBox("먼저 정식 패시브 41개 자동 연결을 실행하세요.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();

            Passive41PrototypeController controller = (Passive41PrototypeController)target;

            if (GUILayout.Button("41종 Registry / 호환 커버리지 검사"))
            {
                controller.ValidateCoverage();
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("현재 검사 결과", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Registry 연결", controller.CoverageRegisteredCount);
                EditorGUILayout.IntField("미지원/중복", controller.CoverageUnsupportedCount);
                EditorGUILayout.IntField("호환 오류", controller.CoverageInvalidCompatibilityCount);
                EditorGUILayout.IntField("Runtime Assigned", controller.ActiveAssignedPassiveCount);
                EditorGUILayout.IntField("Runtime Applied", controller.ActiveAppliedPassiveCount);
                EditorGUILayout.IntField("Runtime Rejected", controller.ActiveRejectedPassiveCount);
                EditorGUILayout.IntField("Runtime Unsupported", controller.ActiveUnsupportedPassiveCount);
                EditorGUILayout.ObjectField("활성 캐릭터", controller.ActiveUnit, typeof(Component), true);
                EditorGUILayout.ObjectField("활성 몬스터", controller.ActiveEnemy, typeof(Component), true);
                EditorGUILayout.TextArea(string.IsNullOrEmpty(controller.LastMessage) ? "결과 없음" : controller.LastMessage);
            }

            EditorGUILayout.Space(8f);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("아래 Runtime 검증 버튼은 Play Mode에서 사용합니다.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("선택 패시브 시나리오 생성")) Execute(controller.SpawnSelectedScenario);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전투 시작")) Execute(controller.StartCombat);
            if (GUILayout.Button("전투 정지")) Execute(controller.StopCombat);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("강제 저지")) Execute(controller.ForceBlockPossibleEnemies);
            if (GUILayout.Button("저지 해제")) Execute(controller.ReleaseAllBlocks);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("패시브 주체 HP 조건 만들기")) Execute(controller.DamageOwnerToConfiguredPercent);
            if (GUILayout.Button("상대 HP 조건 만들기")) Execute(controller.DamagePrimaryOpponentToConfiguredPercent);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("캐릭터 스킬 성공 신호")) Execute(controller.NotifyUnitSkillSucceeded);
            if (GUILayout.Button("캐릭터 회피 성공 Probe")) Execute(controller.SimulateOwnerEvadeSuccess);
            if (GUILayout.Button("주체에 검증용 디버프 주입")) Execute(controller.InjectNegativeStatusToOwner);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("아군 소환물 생성 Probe")) Execute(controller.SpawnUnitSummonSignal);
            if (GUILayout.Button("아군 소환물 해제 Probe")) Execute(controller.ReleaseUnitSummonSignals);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("패시브 주체 사망")) Execute(controller.KillOwner);
            if (GUILayout.Button("주 상대 사망")) Execute(controller.KillPrimaryOpponent);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("패시브 시나리오 초기화")) Execute(controller.ResetScenario);
        }

        private void CollectPassives()
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets/Unit/Data/Passives" });
            List<PassiveDataSO> assets = new List<PassiveDataSO>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                PassiveDataSO passive = AssetDatabase.LoadAssetAtPath<PassiveDataSO>(path);

                if (passive != null)
                {
                    assets.Add(passive);
                }
            }

            assets.Sort((a, b) => string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), System.StringComparison.Ordinal));
            serializedObject.Update();
            officialPassives.arraySize = assets.Count;

            for (int i = 0; i < assets.Count; i++)
            {
                officialPassives.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            }

            selectedPassiveIndex.intValue = Mathf.Clamp(selectedPassiveIndex.intValue, 0, Mathf.Max(0, assets.Count - 1));
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private string[] BuildPassiveNames()
        {
            string[] names = new string[officialPassives.arraySize];

            for (int i = 0; i < officialPassives.arraySize; i++)
            {
                PassiveDataSO passive = officialPassives.GetArrayElementAtIndex(i).objectReferenceValue as PassiveDataSO;
                names[i] = passive == null ? $"[{i}] null" : $"[{i + 1:00}] {passive.DisplayName} ({passive.GetType().Name})";
            }

            return names;
        }

        private void Execute(System.Action action)
        {
            action?.Invoke();
            Repaint();
        }
    }
}
