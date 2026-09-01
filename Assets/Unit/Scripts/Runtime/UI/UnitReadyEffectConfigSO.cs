using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [CreateAssetMenu(menuName = "Endless Guard/Unit/SP Ready Effect Config", fileName = "UnitReadyEffectConfig")]
    public sealed class UnitReadyEffectConfigSO : ScriptableObject
    {
        [Header("SP MAX 셰이더 오라")]
        [Tooltip("캐릭터 실루엣 주변에 그릴 공통 오라 Material입니다. Material의 색/광도/불꽃 강도를 바꾸면 모든 유닛에 적용됩니다.")]
        [SerializeField] private Material auraMaterial;

        [Tooltip("유닛 루트 기준 오라 중심 위치입니다. 파티클 EffectPoint를 사용하지 않으므로 위로 떠다니지 않습니다.")]
        [SerializeField] private Vector3 localPosition = new Vector3(0f, 0.92f, 0f);

        [Tooltip("화면에서 보이는 오라의 가로/세로 크기(World Unit)입니다.")]
        [SerializeField] private Vector2 auraSize = new Vector2(1.30f, 2.15f);

        [Tooltip("캐릭터 바로 뒤에 그리기 위한 Sorting Order입니다.")]
        [SerializeField] private int sortingOrder = -20;

        [Header("오라 레이어")]
        [Tooltip("안쪽 핵심 오라 크기 배율입니다.")]
        [SerializeField, Min(0.1f)] private float coreLayerScale = 1f;

        [Tooltip("바깥쪽 잔광 레이어 크기 배율입니다.")]
        [SerializeField, Min(0.1f)] private float outerLayerScale = 1.08f;

        [Tooltip("바깥쪽 잔광의 투명도입니다.")]
        [SerializeField, Range(0f, 1f)] private float outerLayerAlpha = 0.42f;

        public Material AuraMaterial => auraMaterial;
        public Vector3 LocalPosition => localPosition;
        public Vector2 AuraSize => auraSize;
        public int SortingOrder => sortingOrder;
        public float CoreLayerScale => coreLayerScale;
        public float OuterLayerScale => outerLayerScale;
        public float OuterLayerAlpha => outerLayerAlpha;
    }
}
