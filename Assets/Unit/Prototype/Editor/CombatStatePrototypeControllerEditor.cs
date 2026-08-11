using EndlessGuard.Unit.Prototype;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(CombatStatePrototypeController))]
    public sealed class CombatStatePrototypeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CombatStatePrototypeController controller = (CombatStatePrototypeController)target;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("런타임 검증 상태", EditorStyles.boldLabel);

            DrawRuntimeState(controller);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("아래 검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("검증 대상 다시 생성"))
            {
                Execute(controller.SpawnActors);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("캐릭터 검증", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("캐릭터 피해"))
            {
                Execute(controller.DamageUnit);
            }

            if (GUILayout.Button("캐릭터 회복"))
            {
                Execute(controller.HealUnit);
            }

            if (GUILayout.Button("캐릭터 사망"))
            {
                Execute(controller.KillUnit);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("캐릭터 SP 증가"))
            {
                Execute(controller.AddUnitSkillGauge);
            }

            if (GUILayout.Button("캐릭터 SP 소모"))
            {
                Execute(controller.ConsumeUnitSkillGauge);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("캐릭터 공격 진행"))
            {
                Execute(controller.AdvanceUnitAttackProgress);
            }

            if (GUILayout.Button("캐릭터 준비 공격 소비"))
            {
                Execute(controller.ConsumeUnitReadyAttacks);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("몬스터 검증", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("몬스터 피해"))
            {
                Execute(controller.DamageEnemy);
            }

            if (GUILayout.Button("몬스터 회복"))
            {
                Execute(controller.HealEnemy);
            }

            if (GUILayout.Button("몬스터 사망"))
            {
                Execute(controller.KillEnemy);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("몬스터 공격 진행"))
            {
                Execute(controller.AdvanceEnemyAttackProgress);
            }

            if (GUILayout.Button("몬스터 준비 공격 소비"))
            {
                Execute(controller.ConsumeEnemyReadyAttacks);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("검증 대상 제거"))
            {
                Execute(controller.DespawnActors);
            }
        }

        private static void DrawRuntimeState(CombatStatePrototypeController controller)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                GameObject unitObject = controller.SpawnedUnit == null ? null : controller.SpawnedUnit.gameObject;
                GameObject enemyObject = controller.SpawnedEnemy == null ? null : controller.SpawnedEnemy.gameObject;

                EditorGUILayout.ObjectField(new GUIContent("생성된 캐릭터"), unitObject, typeof(GameObject), true);
                EditorGUILayout.TextField(new GUIContent("캐릭터 HP"), GetUnitHealthText(controller));
                EditorGUILayout.TextField(new GUIContent("캐릭터 SP"), GetUnitSkillGaugeText(controller));
                EditorGUILayout.TextField(new GUIContent("캐릭터 공격 진행도"), GetUnitAttackProgressText(controller));
                EditorGUILayout.IntField(new GUIContent("캐릭터 준비 공격 수"), controller.SpawnedUnit == null ? 0 : controller.SpawnedUnit.ReadyAttackCount);
                EditorGUILayout.IntField(new GUIContent("캐릭터 누적 공격 소비"), controller.UnitConsumedAttackCount);
                EditorGUILayout.IntField(new GUIContent("캐릭터 HP 변경 이벤트"), controller.UnitHealthChangedCount);
                EditorGUILayout.IntField(new GUIContent("캐릭터 SP 변경 이벤트"), controller.UnitSkillGaugeChangedCount);
                EditorGUILayout.IntField(new GUIContent("OnUnitDied 발생 횟수"), controller.UnitDeathEventCount);

                EditorGUILayout.Space(4f);

                EditorGUILayout.ObjectField(new GUIContent("생성된 몬스터"), enemyObject, typeof(GameObject), true);
                EditorGUILayout.TextField(new GUIContent("몬스터 HP"), GetEnemyHealthText(controller));
                EditorGUILayout.TextField(new GUIContent("몬스터 공격 진행도"), GetEnemyAttackProgressText(controller));
                EditorGUILayout.IntField(new GUIContent("몬스터 준비 공격 수"), controller.SpawnedEnemy == null ? 0 : controller.SpawnedEnemy.ReadyAttackCount);
                EditorGUILayout.IntField(new GUIContent("몬스터 누적 공격 소비"), controller.EnemyConsumedAttackCount);
                EditorGUILayout.IntField(new GUIContent("몬스터 HP 변경 이벤트"), controller.EnemyHealthChangedCount);
                EditorGUILayout.IntField(new GUIContent("OnEnemyDied 발생 횟수"), controller.EnemyDeathEventCount);

                EditorGUILayout.Space(4f);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(controller.LastEventMessage) ? "발생한 검증 이벤트가 없습니다." : controller.LastEventMessage);
            }
        }

        private static string GetUnitHealthText(CombatStatePrototypeController controller)
        {
            if (controller.SpawnedUnit == null || controller.SpawnedUnit.Health == null)
            {
                return "캐릭터 없음";
            }

            return $"{controller.SpawnedUnit.Health.CurrentHp:0.##} / {controller.SpawnedUnit.Health.MaxHp:0.##}";
        }

        private static string GetUnitSkillGaugeText(CombatStatePrototypeController controller)
        {
            if (controller.SpawnedUnit == null)
            {
                return "캐릭터 없음";
            }

            return $"{controller.SpawnedUnit.CurrentSkillGauge:0.##} / {controller.SpawnedUnit.MaxSkillGauge:0.##}";
        }

        private static string GetUnitAttackProgressText(CombatStatePrototypeController controller)
        {
            return controller.SpawnedUnit == null ? "캐릭터 없음" : $"{controller.SpawnedUnit.AttackProgress:0.###}";
        }

        private static string GetEnemyHealthText(CombatStatePrototypeController controller)
        {
            if (controller.SpawnedEnemy == null || controller.SpawnedEnemy.Health == null)
            {
                return "몬스터 없음";
            }

            return $"{controller.SpawnedEnemy.Health.CurrentHp:0.##} / {controller.SpawnedEnemy.Health.MaxHp:0.##}";
        }

        private static string GetEnemyAttackProgressText(CombatStatePrototypeController controller)
        {
            return controller.SpawnedEnemy == null ? "몬스터 없음" : $"{controller.SpawnedEnemy.AttackProgress:0.###}";
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}