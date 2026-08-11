using UnityEngine;
using UnityEngine.UI;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BurstGraphic : MaskableGraphic
    {
        [Header("폭발형 외곽선")]
        [Tooltip("폭발 외곽에 생성되는 큰 가시의 개수입니다.")]
        [Range(6, 24)]
        [SerializeField] private int spikeCount = 14;

        [Tooltip("큰 가시 사이로 들어가는 안쪽 꼭짓점의 깊이입니다.")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float innerSpikeRatio = 0.72f;

        [Tooltip("폭발형 외곽선의 두께입니다.")]
        [Min(1f)]
        [SerializeField] private float outlineWidth = 5f;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;
            int pointCount = Mathf.Max(6, spikeCount) * 2;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i < pointCount; i++)
            {
                float angle = (i / (float)pointCount) * Mathf.PI * 2f - Mathf.PI * 0.5f;
                float radiusRatio = i % 2 == 0 ? 1f : innerSpikeRatio;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float outerWidth = halfWidth * radiusRatio;
                float outerHeight = halfHeight * radiusRatio;
                float innerWidth = Mathf.Max(0f, outerWidth - outlineWidth);
                float innerHeight = Mathf.Max(0f, outerHeight - outlineWidth);

                Vector2 innerPosition = center + new Vector2(direction.x * innerWidth, direction.y * innerHeight);
                Vector2 outerPosition = center + new Vector2(direction.x * outerWidth, direction.y * outerHeight);

                vertex.position = innerPosition;
                vertexHelper.AddVert(vertex);

                vertex.position = outerPosition;
                vertexHelper.AddVert(vertex);
            }

            for (int i = 0; i < pointCount; i++)
            {
                int nextIndex = (i + 1) % pointCount;
                int innerCurrent = i * 2;
                int outerCurrent = innerCurrent + 1;
                int innerNext = nextIndex * 2;
                int outerNext = innerNext + 1;

                vertexHelper.AddTriangle(innerCurrent, outerCurrent, outerNext);
                vertexHelper.AddTriangle(innerCurrent, outerNext, innerNext);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            spikeCount = Mathf.Clamp(spikeCount, 6, 24);
            innerSpikeRatio = Mathf.Clamp(innerSpikeRatio, 0.5f, 0.95f);
            outlineWidth = Mathf.Max(1f, outlineWidth);
            SetVerticesDirty();
        }
#endif
    }
}