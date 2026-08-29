using UnityEngine;

// 가챠 뽑기 비용, 등급별 확률 가중치, 천장 스택 기준값 및 구간별 확률을 통합 관리하는 ScriptableObject 설정 에셋
[CreateAssetMenu(fileName = "GachaConfig", menuName = "EndlessGuard/Gacha/GachaConfig", order = 1)]
public class GachaConfigSO : ScriptableObject
{
    #region 가챠 다이아 소모 비용 설정

    [Header("--- 가챠 다이아 비용 설정 ---")]
    [Tooltip("1회 단차 가챠 소모 다이아 수량")]
    [SerializeField] private int singleDrawCost = 300;

    [Tooltip("10회 연속 가챠 소모 다이아 수량")]
    [SerializeField] private int tenDrawCost = 3000;

    #endregion

    #region 등급별 기본 확률 가중치 설정

    [Header("--- 등급별 기본 확률 가중치 (총합 100.0%) ---")]
    [Tooltip("1성 유닛 기본 가중치 (%)")]
    [SerializeField] private float oneStarWeight = 30.0f;

    [Tooltip("2성 유닛 기본 가중치 (%)")]
    [SerializeField] private float twoStarWeight = 30.0f;

    [Tooltip("3성 유닛 기본 가중치 (%)")]
    [SerializeField] private float threeStarWeight = 25.0f;

    [Tooltip("4성 유닛 기본 가중치 (%)")]
    [SerializeField] private float fourStarWeight = 12.0f;

    [Tooltip("5성 유닛 기본 가중치 (%)")]
    [SerializeField] private float fiveStarWeight = 2.9f;

    [Tooltip("6성 유닛 기본 가중치 (%)")]
    [SerializeField] private float sixStarWeight = 0.1f;

    #endregion

    #region 천장(Pity) 스택 및 구간별 확률 설정

    [Header("--- 천장(Pity) 시스템 설정 ---")]
    [Tooltip("소프트 천장 진입 스택 기준값 (50회)")]
    [SerializeField] private int softPityThreshold = 50;

    [Tooltip("하드 천장(6성 확정) 진입 스택 기준값 (100회)")]
    [SerializeField] private int hardPityThreshold = 100;

    [Tooltip("기본 6성 확률 (0.1% = 0.001f)")]
    [SerializeField] private float baseSixStarRate = 0.001f;

    [Tooltip("소프트 천장 진입 시 6성 확률 (10.0% = 0.10f)")]
    [SerializeField] private float softPityRate = 0.10f;

    [Tooltip("하드 천장 진입 시 6성 확정 확률 (100.0% = 1.0f)")]
    [SerializeField] private float hardPityRate = 1.0f;

    #endregion

    #region 프로퍼티 (읽기 전용 인터페이스)

    public int SingleDrawCost => Mathf.Max(1, singleDrawCost);
    public int TenDrawCost => Mathf.Max(1, tenDrawCost);

    public float OneStarWeight => oneStarWeight;
    public float TwoStarWeight => twoStarWeight;
    public float ThreeStarWeight => threeStarWeight;
    public float FourStarWeight => fourStarWeight;
    public float FiveStarWeight => fiveStarWeight;
    public float SixStarWeight => sixStarWeight;

    // 1성~5성 비(非)6성 등급의 가중치 총합 반환 (정규화 연산의 분모로 활용)
    public float NonSixStarWeightTotal => oneStarWeight + twoStarWeight + threeStarWeight + fourStarWeight + fiveStarWeight;

    public int SoftPityThreshold => softPityThreshold;
    public int HardPityThreshold => hardPityThreshold;
    public float BaseSixStarRate => baseSixStarRate;
    public float SoftPityRate => softPityRate;
    public float HardPityRate => hardPityRate;

    #endregion

    #region 헬퍼 메서드

    // 1성~5성 기본 가중치를 배열 형태로 반환 (인덱스 0: 1성 ~ 인덱스 4: 5성)
    public float[] GetNonSixStarBaseWeights()
    {
        return new float[] { oneStarWeight, twoStarWeight, threeStarWeight, fourStarWeight, fiveStarWeight };
    }

    #endregion
}
