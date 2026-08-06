using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Prototype;
using EndlessGuard.Unit.Runtime;
using UnityEditor;
using UnityEngine;

namespace EndlessGuard.Unit.Editor
{
    [CustomEditor(typeof(BasicAttackPrototypeController))]
    public sealed class BasicAttackPrototypeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BasicAttackPrototypeController controller = (BasicAttackPrototypeController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox("상대 타일, 월드 거리, 바라보는 방향과 대상 유형은 실제 생성된 인스턴스의 CombatGridPosition과 Transform 위치를 사용해 자동 계산합니다.", MessageType.Info);
            EditorGUILayout.LabelField("기본 공격 검증 상태", EditorStyles.boldLabel);

            DrawCurrentState(controller);
            DrawLastResult(controller);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("검증 버튼은 Play 상태에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("캐릭터 기본 공격", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("캐릭터 공격 1회 준비"))
            {
                Execute(controller.PrepareUnitAttack);
            }

            if (GUILayout.Button("캐릭터 → 몬스터 공격"))
            {
                Execute(controller.ExecuteUnitAttack);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("몬스터 기본 공격", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("몬스터 공격 1회 준비"))
            {
                Execute(controller.PrepareEnemyAttack);
            }

            if (GUILayout.Button("몬스터 → 캐릭터 공격"))
            {
                Execute(controller.ExecuteEnemyAttack);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5f);

            if (GUILayout.Button("공격 결과 초기화"))
            {
                Execute(controller.ResetResults);
            }
        }

        private static void DrawCurrentState(BasicAttackPrototypeController controller)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("현재 캐릭터"), controller.Unit == null ? null : controller.Unit.gameObject, typeof(GameObject), true);
                EditorGUILayout.TextField(new GUIContent("캐릭터 HP"), GetUnitHealthText(controller));
                EditorGUILayout.TextField(new GUIContent("캐릭터 SP"), GetUnitSkillGaugeText(controller));
                EditorGUILayout.TextField(new GUIContent("캐릭터 공격 진행도"), controller.Unit == null ? "캐릭터 없음" : $"{controller.Unit.AttackProgress:0.###}");
                EditorGUILayout.IntField(new GUIContent("캐릭터 공격 성공 횟수"), controller.UnitAttackSuccessCount);

                bool hasUnitContext = controller.TryCreateUnitAttackContext(out BasicAttackContext unitContext);
                DrawAutomaticContext("캐릭터 자동 공격 상황", hasUnitContext, unitContext, GetUnitAttackSettings(controller));

                EditorGUILayout.Space(4f);

                EditorGUILayout.ObjectField(new GUIContent("현재 몬스터"), controller.Enemy == null ? null : controller.Enemy.gameObject, typeof(GameObject), true);
                EditorGUILayout.TextField(new GUIContent("몬스터 HP"), GetEnemyHealthText(controller));
                EditorGUILayout.TextField(new GUIContent("몬스터 공격 진행도"), controller.Enemy == null ? "몬스터 없음" : $"{controller.Enemy.AttackProgress:0.###}");
                EditorGUILayout.IntField(new GUIContent("몬스터 공격 성공 횟수"), controller.EnemyAttackSuccessCount);

                bool hasEnemyContext = controller.TryCreateEnemyAttackContext(out BasicAttackContext enemyContext);
                DrawAutomaticContext("몬스터 자동 공격 상황", hasEnemyContext, enemyContext, GetEnemyAttackSettings(controller));

                EditorGUILayout.Space(4f);
                EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(controller.LastMessage) ? "기본 공격 검증 결과가 없습니다." : controller.LastMessage);
            }
        }

        private static void DrawAutomaticContext(string label, bool hasContext, BasicAttackContext context, AttackSettings attackSettings)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            if (!hasContext)
            {
                EditorGUILayout.TextField(new GUIContent("자동 계산 상태"), "격자 상태 확인 불가");
                return;
            }

            EditorGUILayout.Vector2IntField(new GUIContent("자동 상대 타일"), context.RelativeTargetTile);
            EditorGUILayout.FloatField(new GUIContent("자동 월드 거리"), context.HorizontalWorldDistance);
            EditorGUILayout.EnumPopup(new GUIContent("자동 바라보는 방향"), context.FacingDirection);
            EditorGUILayout.EnumPopup(new GUIContent("자동 대상 유형"), context.TargetLayer);

            if (attackSettings == null)
            {
                EditorGUILayout.TextField(new GUIContent("패턴 기준 변환 타일"), "공격 데이터 없음");
                return;
            }

            Vector2Int evaluatedTile = BasicAttackRangeEvaluator.ConvertWorldTileToPatternTile(context.RelativeTargetTile, attackSettings.RangeRotationMode, context.FacingDirection);
            EditorGUILayout.Vector2IntField(new GUIContent("패턴 기준 변환 타일"), evaluatedTile);
            EditorGUILayout.FloatField(new GUIContent("공격 사거리"), attackSettings.AttackRange);
            EditorGUILayout.EnumPopup(new GUIContent("범위 회전 방식"), attackSettings.RangeRotationMode);
        }

        private static void DrawLastResult(BasicAttackPrototypeController controller)
        {
            if (!controller.HasResult)
            {
                return;
            }

            BasicAttackResult result = controller.LastResult;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("최근 기본 공격 결과", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(new GUIContent("공격 성공"), result.Succeeded);
                EditorGUILayout.EnumPopup(new GUIContent("실패 원인"), result.FailureReason);
                EditorGUILayout.EnumPopup(new GUIContent("피해 유형"), result.DamageType);
                EditorGUILayout.FloatField(new GUIContent("사용 공격력"), result.AttackPower);
                EditorGUILayout.FloatField(new GUIContent("사용 방어력"), result.Defense);
                EditorGUILayout.FloatField(new GUIContent("계산 피해"), result.CalculatedDamage);
                EditorGUILayout.FloatField(new GUIContent("적용 피해"), result.AppliedDamage);
                EditorGUILayout.FloatField(new GUIContent("획득 SP"), result.SkillGaugeGained);
                EditorGUILayout.Toggle(new GUIContent("대상 사망"), result.TargetDied);
            }
        }

        private static AttackSettings GetUnitAttackSettings(BasicAttackPrototypeController controller)
        {
            return controller.Unit == null || controller.Unit.DataLink == null || !controller.Unit.DataLink.HasData ? null : controller.Unit.DataLink.UnitData.AttackSettings;
        }

        private static AttackSettings GetEnemyAttackSettings(BasicAttackPrototypeController controller)
        {
            return controller.Enemy == null || controller.Enemy.DataLink == null || !controller.Enemy.DataLink.HasData ? null : controller.Enemy.DataLink.EnemyData.AttackSettings;
        }

        private static string GetUnitHealthText(BasicAttackPrototypeController controller)
        {
            return controller.Unit == null || controller.Unit.Health == null ? "캐릭터 없음" : $"{controller.Unit.Health.CurrentHp:0.##} / {controller.Unit.Health.MaxHp:0.##}";
        }

        private static string GetUnitSkillGaugeText(BasicAttackPrototypeController controller)
        {
            return controller.Unit == null ? "캐릭터 없음" : $"{controller.Unit.CurrentSkillGauge:0.##} / {controller.Unit.MaxSkillGauge:0.##}";
        }

        private static string GetEnemyHealthText(BasicAttackPrototypeController controller)
        {
            return controller.Enemy == null || controller.Enemy.Health == null ? "몬스터 없음" : $"{controller.Enemy.Health.CurrentHp:0.##} / {controller.Enemy.Health.MaxHp:0.##}";
        }

        private void Execute(System.Action action)
        {
            action.Invoke();
            Repaint();
        }
    }
}