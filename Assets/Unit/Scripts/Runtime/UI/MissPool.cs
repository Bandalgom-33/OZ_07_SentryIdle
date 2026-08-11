using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class MissPool : MonoBehaviour
    {
        [Header("MISS 풀")]
        [Tooltip("풀에서 재사용할 MISS 표시 프리팹입니다.")]
        [SerializeField] private MissFeedback missPrefab;

        [Tooltip("월드 위치를 화면 좌표로 변환할 카메라입니다.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("전투 시작 시 미리 생성할 MISS 표시 개수입니다.")]
        [Min(1)]
        [SerializeField] private int initialCapacity = 12;

        [Tooltip("같은 대상에게 동시에 표시할 수 있는 MISS 최대 개수입니다.")]
        [Range(1, 10)]
        [SerializeField] private int maxPerTarget = 4;

        private static MissPool instance;

        private readonly List<MissFeedback> availableMisses = new List<MissFeedback>(12);
        private readonly List<MissFeedback> activeMisses = new List<MissFeedback>(12);
        private readonly Dictionary<int, List<MissFeedback>> targetStacks = new Dictionary<int, List<MissFeedback>>();

        private Canvas canvas;
        private RectTransform canvasRect;
        private int createdCount;

        public static MissPool Instance => instance;
        public int ActiveCount => activeMisses.Count;
        public int AvailableCount => availableMisses.Count;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("MissPool이 씬에 둘 이상 존재합니다.", this);
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
                Debug.LogError("MissPool은 RectTransform이 있는 Canvas에 배치해야 합니다.", this);
                enabled = false;
                return;
            }

            Prewarm();
        }

        private void OnEnable()
        {
            CombatFeedbackEvents.OnAttackMissed += HandleAttackMissed;
        }

        private void OnDisable()
        {
            CombatFeedbackEvents.OnAttackMissed -= HandleAttackMissed;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnValidate()
        {
            initialCapacity = Mathf.Max(1, initialCapacity);
            maxPerTarget = Mathf.Clamp(maxPerTarget, 1, 10);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = activeMisses.Count - 1; i >= 0; i--)
            {
                MissFeedback feedback = activeMisses[i];

                if (feedback != null && feedback.Step(deltaTime))
                {
                    continue;
                }

                activeMisses.RemoveAt(i);

                if (feedback == null)
                {
                    continue;
                }

                RemoveFromTargetStack(feedback);
                feedback.Hide();
                availableMisses.Add(feedback);
            }
        }

        private void HandleAttackMissed(CombatHealth target, Vector3 worldPosition)
        {
            ShowInternal(target, worldPosition);
        }

        private void Prewarm()
        {
            if (missPrefab == null)
            {
                Debug.LogError("MissPool에 MissFeedback 프리팹이 연결되지 않았습니다.", this);
                return;
            }

            for (int i = 0; i < initialCapacity; i++)
            {
                availableMisses.Add(CreateMiss());
            }
        }

        private bool ShowInternal(CombatHealth target, Vector3 worldPosition)
        {
            if (target == null || missPrefab == null || canvas == null || canvasRect == null)
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
            List<MissFeedback> stack = GetTargetStack(targetId);

            CleanStack(stack);

            if (stack.Count >= maxPerTarget)
            {
                RecycleOldest(stack);
            }

            PushExisting(stack);

            MissFeedback feedback = GetMiss();

            if (feedback == null)
            {
                return false;
            }

            feedback.transform.SetAsLastSibling();
            feedback.Show(anchoredPosition, targetId);
            activeMisses.Add(feedback);
            stack.Add(feedback);
            return true;
        }

        private void PushExisting(List<MissFeedback> stack)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                MissFeedback feedback = stack[i];

                if (feedback != null && feedback.IsPlaying)
                {
                    feedback.PushUp();
                }
            }
        }

        private void CleanStack(List<MissFeedback> stack)
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                MissFeedback feedback = stack[i];

                if (feedback == null || !feedback.IsPlaying)
                {
                    stack.RemoveAt(i);
                }
            }
        }

        private void RecycleOldest(List<MissFeedback> stack)
        {
            if (stack.Count == 0)
            {
                return;
            }

            MissFeedback oldest = stack[0];
            stack.RemoveAt(0);

            if (oldest == null)
            {
                return;
            }

            activeMisses.Remove(oldest);
            oldest.Hide();
            availableMisses.Add(oldest);
        }

        private List<MissFeedback> GetTargetStack(int targetId)
        {
            if (targetStacks.TryGetValue(targetId, out List<MissFeedback> stack))
            {
                return stack;
            }

            stack = new List<MissFeedback>(maxPerTarget);
            targetStacks.Add(targetId, stack);
            return stack;
        }

        private void RemoveFromTargetStack(MissFeedback feedback)
        {
            int targetId = feedback.TargetId;

            if (targetId == 0 || !targetStacks.TryGetValue(targetId, out List<MissFeedback> stack))
            {
                return;
            }

            stack.Remove(feedback);

            if (stack.Count == 0)
            {
                targetStacks.Remove(targetId);
            }
        }

        private MissFeedback GetMiss()
        {
            int lastIndex = availableMisses.Count - 1;

            if (lastIndex >= 0)
            {
                MissFeedback feedback = availableMisses[lastIndex];
                availableMisses.RemoveAt(lastIndex);
                return feedback;
            }

            if (activeMisses.Count == 0)
            {
                return null;
            }

            MissFeedback oldest = activeMisses[0];
            activeMisses.RemoveAt(0);
            RemoveFromTargetStack(oldest);
            oldest.Hide();
            return oldest;
        }

        private MissFeedback CreateMiss()
        {
            MissFeedback feedback = Instantiate(missPrefab, canvasRect);
            createdCount++;
            feedback.name = $"{missPrefab.name}_{createdCount:00}";
            feedback.Prepare();
            feedback.Hide();
            return feedback;
        }
    }
}