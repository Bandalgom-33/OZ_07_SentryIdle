using EndlessGuard.Unit.Data;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class DamageNumberPool : MonoBehaviour
    {
        [Header("피해 숫자 풀")]
        [Tooltip("풀에서 재사용할 피해 숫자 프리팹입니다.")]
        [SerializeField] private DamageNumber numberPrefab;

        [Tooltip("전투 월드 위치를 화면 좌표로 변환할 카메라입니다. 비어 있으면 시작 시 MainCamera를 한 번 찾습니다.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("전투 시작 시 미리 생성할 피해 숫자 개수입니다.")]
        [Min(1)]
        [SerializeField] private int initialCapacity = 32;

        [Tooltip("같은 대상에게 동시에 표시할 수 있는 피해 숫자의 최대 개수입니다.")]
        [Range(1, 20)]
        [SerializeField] private int maxNumbersPerTarget = 6;

        private static DamageNumberPool instance;

        private readonly List<DamageNumber> availableNumbers = new List<DamageNumber>(32);
        private readonly List<DamageNumber> activeNumbers = new List<DamageNumber>(32);
        private readonly Dictionary<int, List<DamageNumber>> targetStacks = new Dictionary<int, List<DamageNumber>>();

        private Canvas canvas;
        private RectTransform canvasRect;
        private int createdCount;

        public static DamageNumberPool Instance => instance;
        public int ActiveCount => activeNumbers.Count;
        public int AvailableCount => availableNumbers.Count;
        public int MaxNumbersPerTarget => maxNumbersPerTarget;
        public int MaxLayers => maxNumbersPerTarget;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("DamageNumberPool이 씬에 둘 이상 존재합니다.", this);
                enabled = false;
                return;
            }

            instance = this;
            canvas = GetComponent<Canvas>();
            canvasRect = GetComponent<RectTransform>();

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (canvasRect == null)
            {
                Debug.LogError("DamageNumberPool은 RectTransform이 있는 Canvas 오브젝트에 배치해야 합니다.", this);
                enabled = false;
                return;
            }

            Prewarm();
        }

        private void OnValidate()
        {
            initialCapacity = Mathf.Max(1, initialCapacity);
            maxNumbersPerTarget = Mathf.Clamp(maxNumbersPerTarget, 1, 20);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = activeNumbers.Count - 1; i >= 0; i--)
            {
                DamageNumber number = activeNumbers[i];

                if (number != null && number.Step(deltaTime))
                {
                    continue;
                }

                activeNumbers.RemoveAt(i);

                if (number == null)
                {
                    continue;
                }

                RemoveFromTargetStack(number);
                number.Hide();
                availableNumbers.Add(number);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static bool Show(CombatHealth target, float damage, Vector3 worldPosition)
        {
            DamageInfo damageInfo = new DamageInfo(damage, DamageType.None, false);
            return Show(target, damageInfo, worldPosition, DamageNumberTargetType.Enemy);
        }

        public static bool Show(CombatHealth target, float damage, Vector3 worldPosition, DamageNumberTargetType targetType)
        {
            DamageInfo damageInfo = new DamageInfo(damage, DamageType.None, false);
            return Show(target, damageInfo, worldPosition, targetType);
        }

        public static bool Show(CombatHealth target, DamageInfo damageInfo, Vector3 worldPosition)
        {
            return Show(target, damageInfo, worldPosition, DamageNumberTargetType.Enemy);
        }

        public static bool Show(CombatHealth target, DamageInfo damageInfo, Vector3 worldPosition, DamageNumberTargetType targetType)
        {
            return instance != null && instance.ShowInternal(target, damageInfo, worldPosition, targetType);
        }

        private void Prewarm()
        {
            if (numberPrefab == null)
            {
                Debug.LogError("DamageNumberPool에 DamageNumber 프리팹이 연결되지 않았습니다.", this);
                return;
            }

            for (int i = 0; i < initialCapacity; i++)
            {
                availableNumbers.Add(CreateNumber());
            }
        }

        private bool ShowInternal(CombatHealth target, DamageInfo damageInfo, Vector3 worldPosition, DamageNumberTargetType targetType)
        {
            if (target == null || damageInfo.FinalDamage <= 0f || numberPrefab == null || canvas == null || canvasRect == null)
            {
                return false;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;

                if (worldCamera == null)
                {
                    return false;
                }
            }

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z <= 0f)
            {
                return false;
            }

            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvasCamera, out Vector2 anchoredPosition))
            {
                return false;
            }

            int targetId = target.GetInstanceID();
            List<DamageNumber> stack = GetTargetStack(targetId);

            CleanStack(stack);

            if (stack.Count >= maxNumbersPerTarget)
            {
                RecycleOldest(stack);
            }

            PushExistingNumbers(stack);

            DamageNumber number = GetNumber();

            if (number == null)
            {
                return false;
            }

            number.Show(damageInfo, anchoredPosition, targetId, targetType);

            if (damageInfo.IsCritical)
            {
                BurstPool.Show(number);
            }

            activeNumbers.Add(number);
            stack.Add(number);
            return true;
        }

        private void PushExistingNumbers(List<DamageNumber> stack)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                DamageNumber number = stack[i];

                if (number == null || !number.IsPlaying)
                {
                    continue;
                }

                number.PushUp();
            }
        }

        private void CleanStack(List<DamageNumber> stack)
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                DamageNumber number = stack[i];

                if (number == null || !number.IsPlaying)
                {
                    stack.RemoveAt(i);
                }
            }
        }

        private void RecycleOldest(List<DamageNumber> stack)
        {
            if (stack.Count == 0)
            {
                return;
            }

            DamageNumber oldest = stack[0];
            stack.RemoveAt(0);

            if (oldest == null)
            {
                return;
            }

            activeNumbers.Remove(oldest);
            oldest.Hide();
            availableNumbers.Add(oldest);
        }

        private List<DamageNumber> GetTargetStack(int targetId)
        {
            if (targetStacks.TryGetValue(targetId, out List<DamageNumber> stack))
            {
                return stack;
            }

            stack = new List<DamageNumber>(maxNumbersPerTarget);
            targetStacks.Add(targetId, stack);
            return stack;
        }

        private void RemoveFromTargetStack(DamageNumber number)
        {
            int targetId = number.TargetId;

            if (targetId == 0 || !targetStacks.TryGetValue(targetId, out List<DamageNumber> stack))
            {
                return;
            }

            stack.Remove(number);

            if (stack.Count == 0)
            {
                targetStacks.Remove(targetId);
            }
        }

        private DamageNumber GetNumber()
        {
            int lastIndex = availableNumbers.Count - 1;

            if (lastIndex >= 0)
            {
                DamageNumber number = availableNumbers[lastIndex];
                availableNumbers.RemoveAt(lastIndex);
                return number;
            }

            return CreateNumber();
        }

        private DamageNumber CreateNumber()
        {
            DamageNumber number = Instantiate(numberPrefab, canvasRect);
            createdCount++;
            number.name = $"{numberPrefab.name}_{createdCount:00}";
            number.Prepare();
            number.Hide();
            return number;
        }
    }
}