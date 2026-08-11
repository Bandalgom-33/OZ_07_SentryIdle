using System.Collections.Generic;
using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class BurstPool : MonoBehaviour
    {
        [Header("치명타 VFX 풀")]
        [Tooltip("풀에서 재사용할 치명타 폭발 VFX 프리팹입니다.")]
        [SerializeField] private CriticalBurst burstPrefab;

        [Tooltip("전투 시작 시 미리 생성할 치명타 VFX 개수입니다.")]
        [Min(1)]
        [SerializeField] private int initialCapacity = 16;

        private static BurstPool instance;

        private readonly List<CriticalBurst> availableBursts = new List<CriticalBurst>(16);
        private readonly List<CriticalBurst> activeBursts = new List<CriticalBurst>(16);

        private RectTransform canvasRect;
        private int createdCount;

        public static BurstPool Instance => instance;
        public int ActiveCount => activeBursts.Count;
        public int AvailableCount => availableBursts.Count;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("BurstPool이 씬에 둘 이상 존재합니다.", this);
                enabled = false;
                return;
            }

            instance = this;
            canvasRect = GetComponent<RectTransform>();

            if (canvasRect == null)
            {
                Debug.LogError("BurstPool은 RectTransform이 있는 Canvas에 배치해야 합니다.", this);
                enabled = false;
                return;
            }

            Prewarm();
        }

        private void OnValidate()
        {
            initialCapacity = Mathf.Max(1, initialCapacity);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = activeBursts.Count - 1; i >= 0; i--)
            {
                CriticalBurst burst = activeBursts[i];

                if (burst != null && burst.Step(deltaTime))
                {
                    continue;
                }

                activeBursts.RemoveAt(i);

                if (burst == null)
                {
                    continue;
                }

                burst.Hide();
                availableBursts.Add(burst);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static bool Show(DamageNumber followNumber)
        {
            return instance != null && instance.ShowInternal(followNumber);
        }

        private void Prewarm()
        {
            if (burstPrefab == null)
            {
                Debug.LogError("BurstPool에 CriticalBurst 프리팹이 연결되지 않았습니다.", this);
                return;
            }

            for (int i = 0; i < initialCapacity; i++)
            {
                availableBursts.Add(CreateBurst());
            }
        }

        private bool ShowInternal(DamageNumber followNumber)
        {
            if (followNumber == null || burstPrefab == null || canvasRect == null)
            {
                return false;
            }

            CriticalBurst burst = GetBurst();

            if (burst == null)
            {
                return false;
            }

            burst.transform.SetAsFirstSibling();
            burst.Play(followNumber);
            activeBursts.Add(burst);
            return true;
        }

        private CriticalBurst GetBurst()
        {
            int lastIndex = availableBursts.Count - 1;

            if (lastIndex >= 0)
            {
                CriticalBurst burst = availableBursts[lastIndex];
                availableBursts.RemoveAt(lastIndex);
                return burst;
            }

            if (activeBursts.Count == 0)
            {
                return null;
            }

            CriticalBurst oldest = activeBursts[0];
            activeBursts.RemoveAt(0);

            if (oldest != null)
            {
                oldest.Hide();
            }

            return oldest;
        }

        private CriticalBurst CreateBurst()
        {
            CriticalBurst burst = Instantiate(burstPrefab, canvasRect);
            createdCount++;
            burst.name = $"{burstPrefab.name}_{createdCount:00}";
            burst.Prepare();
            burst.Hide();
            burst.transform.SetAsFirstSibling();
            return burst;
        }
    }
}