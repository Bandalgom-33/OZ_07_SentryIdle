using EndlessGuard.Unit.Data;
using EndlessGuard.Unit.Runtime;
using UnityEngine;

namespace EndlessGuard.Unit.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BlockTest))]
    public sealed class TargetTest : MonoBehaviour
    {
        private static readonly Vector2Int FirstStartOffset = new Vector2Int(0, 2);
        private static readonly Vector2Int FirstGoalOffset = new Vector2Int(0, 8);
        private static readonly Vector2Int SecondStartOffset = new Vector2Int(2, 0);
        private static readonly Vector2Int SecondGoalOffset = new Vector2Int(4, 0);
        private static readonly Vector2Int ThirdStartOffset = new Vector2Int(-1, 0);
        private static readonly Vector2Int ThirdGoalOffset = new Vector2Int(-5, 0);

        [Header("검증 대상 연결")]
        [Tooltip("자동 이동과 저지 상태를 제공하는 검증 컴포넌트입니다.")]
        [SerializeField] private BlockTest blockTest;

        [Header("캐릭터 대상 검증")]
        [Tooltip("캐릭터 대상 탐색 검증에서 사용하는 격자 한 칸의 월드 크기입니다.")]
        [Min(0.01f)]
        [SerializeField] private float tileWorldSize = 1f;

        [HideInInspector]
        [SerializeField] private UnitRuntimeState expectedTarget;

        [HideInInspector]
        [SerializeField] private UnitRuntimeState firstTarget;

        [HideInInspector]
        [SerializeField] private UnitRuntimeState secondTarget;

        [HideInInspector]
        [SerializeField] private UnitRuntimeState thirdTarget;

        [HideInInspector]
        [SerializeField] private bool firstFound;

        [HideInInspector]
        [SerializeField] private bool secondFound;

        [HideInInspector]
        [SerializeField] private bool thirdFound;

        [HideInInspector]
        [SerializeField] private bool firstPassed;

        [HideInInspector]
        [SerializeField] private bool secondPassed;

        [HideInInspector]
        [SerializeField] private bool thirdPassed;

        [HideInInspector]
        [SerializeField] private bool finalPassed;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string resultMessage;

        [HideInInspector]
        [SerializeField] private UnitRuntimeState unitAttacker;

        [HideInInspector]
        [SerializeField] private EnemyRuntimeState expectedEnemyTarget;

        [HideInInspector]
        [SerializeField] private EnemyRuntimeState foundEnemyTarget;

        [HideInInspector]
        [SerializeField] private bool unitTargetReady;

        [HideInInspector]
        [SerializeField] private bool unitTargetFound;

        [HideInInspector]
        [SerializeField] private float firstRemainingDistance;

        [HideInInspector]
        [SerializeField] private float secondRemainingDistance;

        [HideInInspector]
        [SerializeField] private float thirdRemainingDistance;

        [HideInInspector]
        [SerializeField] private GridFacingDirection initialFacing;

        [HideInInspector]
        [SerializeField] private GridFacingDirection finalFacing;

        [HideInInspector]
        [SerializeField] private bool priorityPassed;

        [HideInInspector]
        [SerializeField] private bool facingPassed;

        [HideInInspector]
        [SerializeField] private bool unitFinalPassed;

        [HideInInspector]
        [TextArea(2, 4)]
        [SerializeField] private string unitResultMessage;

        public UnitRuntimeState ExpectedTarget => expectedTarget;
        public UnitRuntimeState FirstTarget => firstTarget;
        public UnitRuntimeState SecondTarget => secondTarget;
        public UnitRuntimeState ThirdTarget => thirdTarget;
        public bool FirstFound => firstFound;
        public bool SecondFound => secondFound;
        public bool ThirdFound => thirdFound;
        public bool FirstPassed => firstPassed;
        public bool SecondPassed => secondPassed;
        public bool ThirdPassed => thirdPassed;
        public bool FinalPassed => finalPassed;
        public string ResultMessage => resultMessage;

        public UnitRuntimeState UnitAttacker => unitAttacker;
        public EnemyRuntimeState ExpectedEnemyTarget => expectedEnemyTarget;
        public EnemyRuntimeState FoundEnemyTarget => foundEnemyTarget;
        public bool UnitTargetReady => unitTargetReady;
        public bool UnitTargetFound => unitTargetFound;
        public float FirstRemainingDistance => firstRemainingDistance;
        public float SecondRemainingDistance => secondRemainingDistance;
        public float ThirdRemainingDistance => thirdRemainingDistance;
        public GridFacingDirection InitialFacing => initialFacing;
        public GridFacingDirection FinalFacing => finalFacing;
        public bool PriorityPassed => priorityPassed;
        public bool FacingPassed => facingPassed;
        public bool UnitFinalPassed => unitFinalPassed;
        public string UnitResultMessage => unitResultMessage;

        private void Reset()
        {
            blockTest = GetComponent<BlockTest>();
        }

        private void OnValidate()
        {
            if (blockTest == null)
            {
                blockTest = GetComponent<BlockTest>();
            }

            tileWorldSize = Mathf.Max(0.01f, tileWorldSize);
        }

        public void VerifyTargets()
        {
            ResetResult();

            if (!CanVerify())
            {
                return;
            }

            expectedTarget = blockTest.UnitBlock.State;

            EnemyRuntimeState firstEnemy = GetEnemyState(blockTest.FirstBlock);
            EnemyRuntimeState secondEnemy = GetEnemyState(blockTest.SecondBlock);
            EnemyRuntimeState thirdEnemy = GetEnemyState(blockTest.ThirdBlock);

            if (firstEnemy == null || secondEnemy == null || thirdEnemy == null)
            {
                resultMessage = "검증 몬스터에서 EnemyRuntimeState를 찾지 못했습니다.";
                Debug.LogError(resultMessage, this);
                return;
            }

            firstFound = EnemyTargetFinder.TryFind(firstEnemy, out firstTarget);
            secondFound = EnemyTargetFinder.TryFind(secondEnemy, out secondTarget);
            thirdFound = EnemyTargetFinder.TryFind(thirdEnemy, out thirdTarget);

            firstPassed = firstFound && firstTarget == expectedTarget;
            secondPassed = secondFound && secondTarget == expectedTarget;
            thirdPassed = !thirdFound && thirdTarget == null;
            finalPassed = firstPassed && secondPassed && thirdPassed;

            resultMessage = finalPassed ? "몬스터 대상 탐색 검증 성공: 저지된 2마리는 조성원을 찾고, 통과한 1마리는 대상을 찾지 못했습니다." : $"몬스터 대상 탐색 검증 실패: 첫 번째 {firstPassed}, 두 번째 {secondPassed}, 세 번째 {thirdPassed}";

            if (finalPassed)
            {
                Debug.Log(resultMessage, this);
            }
            else
            {
                Debug.LogWarning(resultMessage, this);
            }
        }

        public void SetupUnitTarget()
        {
            ResetUnitResult();

            if (blockTest == null)
            {
                FailUnitTarget("BlockTest가 연결되지 않았습니다.");
                return;
            }

            CombatLoop combatLoop = GetComponent<CombatLoop>();

            if (combatLoop != null)
            {
                combatLoop.StopLoop();
            }

            blockTest.Setup();

            if (blockTest.UnitBlock == null || blockTest.UnitBlock.State == null)
            {
                FailUnitTarget("캐릭터 대상 탐색 검증용 캐릭터를 준비하지 못했습니다.");
                return;
            }

            EnemyRuntimeState firstEnemy = GetEnemyState(blockTest.FirstBlock);
            EnemyRuntimeState secondEnemy = GetEnemyState(blockTest.SecondBlock);
            EnemyRuntimeState thirdEnemy = GetEnemyState(blockTest.ThirdBlock);

            if (firstEnemy == null || secondEnemy == null || thirdEnemy == null)
            {
                FailUnitTarget("캐릭터 대상 탐색 검증용 몬스터 3마리를 준비하지 못했습니다.");
                return;
            }

            unitAttacker = blockTest.UnitBlock.State;

            if (unitAttacker.GridPosition == null || !unitAttacker.GridPosition.IsInitialized)
            {
                FailUnitTarget("캐릭터의 격자 상태가 초기화되지 않았습니다.");
                return;
            }

            AttackSettings attackSettings = unitAttacker.DataLink.UnitData.AttackSettings;

            if (attackSettings == null)
            {
                FailUnitTarget("캐릭터 기본 공격 설정이 없습니다.");
                return;
            }

            if (attackSettings.RangeRotationMode != AttackRangeRotationMode.FollowFacing)
            {
                FailUnitTarget("이번 검증은 공격 범위 회전 방식이 '바라보는 방향 따라 회전'일 때 사용합니다.");
                return;
            }

            initialFacing = GridFacingDirection.North;
            unitAttacker.GridPosition.SetFacingDirection(initialFacing);

            if (!SetTestPath(firstEnemy, FirstStartOffset, FirstGoalOffset))
            {
                FailUnitTarget("첫 번째 몬스터의 검증 경로를 설정하지 못했습니다.");
                return;
            }

            if (!SetTestPath(secondEnemy, SecondStartOffset, SecondGoalOffset))
            {
                FailUnitTarget("두 번째 몬스터의 검증 경로를 설정하지 못했습니다.");
                return;
            }

            if (!SetTestPath(thirdEnemy, ThirdStartOffset, ThirdGoalOffset))
            {
                FailUnitTarget("세 번째 몬스터의 검증 경로를 설정하지 못했습니다.");
                return;
            }

            firstRemainingDistance = firstEnemy.Move.RemainingPathDistance;
            secondRemainingDistance = secondEnemy.Move.RemainingPathDistance;
            thirdRemainingDistance = thirdEnemy.Move.RemainingPathDistance;
            expectedEnemyTarget = secondEnemy;
            finalFacing = unitAttacker.GridPosition.FacingDirection;
            unitTargetReady = true;
            unitResultMessage = $"캐릭터 대상 검증 준비 완료: 남은 경로 거리 {firstRemainingDistance:0.##}, {secondRemainingDistance:0.##}, {thirdRemainingDistance:0.##}";
            Debug.Log(unitResultMessage, this);
        }

        public void VerifyUnitTarget()
        {
            if (!unitTargetReady || unitAttacker == null || expectedEnemyTarget == null)
            {
                unitResultMessage = "먼저 캐릭터 대상 검증 준비를 실행하세요.";
                Debug.LogWarning(unitResultMessage, this);
                return;
            }

            unitAttacker.GridPosition.SetFacingDirection(initialFacing);
            unitTargetFound = UnitTargetFinder.TryFind(unitAttacker, out foundEnemyTarget);
            finalFacing = unitAttacker.GridPosition.FacingDirection;

            bool remainingDistancePassed = secondRemainingDistance < firstRemainingDistance && secondRemainingDistance < thirdRemainingDistance;
            priorityPassed = remainingDistancePassed && unitTargetFound && foundEnemyTarget == expectedEnemyTarget;
            facingPassed = finalFacing == GridFacingDirection.East;
            unitFinalPassed = priorityPassed && facingPassed;

            unitResultMessage = unitFinalPassed ? $"캐릭터 대상 탐색 검증 성공: 남은 경로가 가장 짧은 두 번째 몬스터 선택, 방향 {initialFacing} → {finalFacing}" : $"캐릭터 대상 탐색 검증 실패: 대상 우선순위 {priorityPassed}, 방향 변경 {facingPassed}";

            if (unitFinalPassed)
            {
                Debug.Log(unitResultMessage, this);
            }
            else
            {
                Debug.LogWarning(unitResultMessage, this);
            }
        }

        public void ResetResult()
        {
            expectedTarget = null;
            firstTarget = null;
            secondTarget = null;
            thirdTarget = null;
            firstFound = false;
            secondFound = false;
            thirdFound = false;
            firstPassed = false;
            secondPassed = false;
            thirdPassed = false;
            finalPassed = false;
            resultMessage = string.Empty;
        }

        public void ResetUnitResult()
        {
            unitAttacker = null;
            expectedEnemyTarget = null;
            foundEnemyTarget = null;
            unitTargetReady = false;
            unitTargetFound = false;
            firstRemainingDistance = 0f;
            secondRemainingDistance = 0f;
            thirdRemainingDistance = 0f;
            initialFacing = GridFacingDirection.North;
            finalFacing = GridFacingDirection.North;
            priorityPassed = false;
            facingPassed = false;
            unitFinalPassed = false;
            unitResultMessage = string.Empty;
        }

        private bool CanVerify()
        {
            if (blockTest == null)
            {
                resultMessage = "BlockTest가 연결되지 않았습니다.";
                Debug.LogError(resultMessage, this);
                return false;
            }

            if (blockTest.UnitBlock == null || blockTest.UnitBlock.State == null)
            {
                resultMessage = "검증 캐릭터가 준비되지 않았습니다.";
                Debug.LogError(resultMessage, this);
                return false;
            }

            if (blockTest.FirstBlock == null || blockTest.SecondBlock == null || blockTest.ThirdBlock == null)
            {
                resultMessage = "검증 몬스터 3마리가 준비되지 않았습니다.";
                Debug.LogError(resultMessage, this);
                return false;
            }

            if (!blockTest.FirstBlock.IsBlocked || !blockTest.SecondBlock.IsBlocked || blockTest.ThirdBlock.IsBlocked)
            {
                resultMessage = "자동 이동 저지 검증을 먼저 완료해야 합니다.";
                Debug.LogWarning(resultMessage, this);
                return false;
            }

            return true;
        }

        private bool SetTestPath(EnemyRuntimeState enemy, Vector2Int startOffset, Vector2Int goalOffset)
        {
            if (enemy == null || enemy.Move == null || unitAttacker == null)
            {
                return false;
            }

            PathNode[] path = BuildPath(startOffset, goalOffset);

            if (!enemy.Move.SetPath(path))
            {
                return false;
            }

            enemy.Move.SetPaused(true);
            return true;
        }

        private PathNode[] BuildPath(Vector2Int startOffset, Vector2Int goalOffset)
        {
            int xCount = Mathf.Abs(goalOffset.x - startOffset.x);
            int yCount = Mathf.Abs(goalOffset.y - startOffset.y);
            PathNode[] path = new PathNode[xCount + yCount + 1];
            Vector2Int current = startOffset;
            int index = 0;

            path[index++] = CreateNode(current, GetFirstFacing(startOffset, goalOffset));

            while (current.x != goalOffset.x)
            {
                int step = goalOffset.x > current.x ? 1 : -1;
                current = new Vector2Int(current.x + step, current.y);
                path[index++] = CreateNode(current, step > 0 ? GridFacingDirection.East : GridFacingDirection.West);
            }

            while (current.y != goalOffset.y)
            {
                int step = goalOffset.y > current.y ? 1 : -1;
                current = new Vector2Int(current.x, current.y + step);
                path[index++] = CreateNode(current, step > 0 ? GridFacingDirection.North : GridFacingDirection.South);
            }

            return path;
        }

        private PathNode CreateNode(Vector2Int relativeTile, GridFacingDirection facing)
        {
            Vector2Int absoluteTile = unitAttacker.GridPosition.TileCoordinate + relativeTile;
            Vector3 position = unitAttacker.transform.position + new Vector3(relativeTile.x * tileWorldSize, 0f, relativeTile.y * tileWorldSize);
            return new PathNode(position, absoluteTile, facing);
        }

        private static GridFacingDirection GetFirstFacing(Vector2Int startOffset, Vector2Int goalOffset)
        {
            if (goalOffset.x > startOffset.x)
            {
                return GridFacingDirection.East;
            }

            if (goalOffset.x < startOffset.x)
            {
                return GridFacingDirection.West;
            }

            return goalOffset.y >= startOffset.y ? GridFacingDirection.North : GridFacingDirection.South;
        }

        private void FailUnitTarget(string failureMessage)
        {
            unitTargetReady = false;
            unitFinalPassed = false;
            unitResultMessage = failureMessage;
            Debug.LogWarning(unitResultMessage, this);
        }

        private static EnemyRuntimeState GetEnemyState(EnemyBlock block)
        {
            return block == null ? null : block.GetComponent<EnemyRuntimeState>();
        }
    }
}