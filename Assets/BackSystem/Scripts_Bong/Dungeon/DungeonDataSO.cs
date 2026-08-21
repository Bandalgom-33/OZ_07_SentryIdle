using System;
using UnityEngine;

// 던전 기획 메타데이터 ScriptableObject
[CreateAssetMenu(fileName = "DungeonData_", menuName = "EndlessGuard/Dungeon/DungeonDataSO", order = 1)]
public class DungeonDataSO : ScriptableObject
{
    #region 직렬화 변수 (인스펙터 기획 데이터 설정)

    [Header("--- 던전 기본 정보 ---")]
    [Tooltip("던전 고유 식별자 (예: DUNGEON_01, DUNGEON_02, DUNGEON_03)")]
    [SerializeField] private string dungeonId = "DUNGEON_01";

    [Tooltip("UI에 표시될 던전 명칭")]
    [SerializeField] private string dungeonName = "고블린 지하 광산";

    [Tooltip("던전 설명 텍스트")]
    [TextArea(2, 4)]
    [SerializeField] private string description = "비교적 약한 몬스터들이 점거한 광산으로, 유닛들을 파견하여 지속적으로 골드와 마석을 채굴합니다.";

    [Tooltip("던전 대표 썸네일/아이콘 이미지")]
    [SerializeField] private Sprite dungeonIcon;

    [Header("--- 가동 조건 및 생산 주기 설정 ---")]
    [Tooltip("생산 가동을 시작하기 위해 필요한 3명 유닛의 최소 요구 총 전투력")]
    [SerializeField] private int requiredMinCombatPower = 50;

    [Tooltip("보상 1회를 생산하는 데 소요되는 기본 기준 시간 (초 단위, 예: 30초, 60초)")]
    [SerializeField] private float baseCycleSeconds = 30.0f;

    [Header("--- 1회 생산 기준 기본 보상량 ---")]
    [Tooltip("1회 완료 시 지급되는 기본 골드 수량")]
    [SerializeField] private long baseRewardGold = 500;

    [Tooltip("1회 완료 시 지급되는 기본 다이아 수량")]
    [SerializeField] private long baseRewardDiamond = 1;

    [Tooltip("1회 완료 시 지급되는 기본 스테이지 마석 수량")]
    [SerializeField] private long baseRewardStageStone = 1;

    #endregion

    #region 읽기 전용 프로퍼티

    // 던전 식별 ID 반환
    public string DungeonId => dungeonId;

    // 던전 명칭 반환
    public string DungeonName => dungeonName;

    // 던전 설명 텍스트 반환
    public string Description => description;

    // 던전 아이콘 스프라이트 반환
    public Sprite DungeonIcon => dungeonIcon;

    // 요구 최소 전투력 반환
    public int RequiredMinCombatPower => Mathf.Max(1, requiredMinCombatPower);

    // 1회 생산 기준 시간 반환
    public float BaseCycleSeconds => Mathf.Max(1.0f, baseCycleSeconds);

    // 기본 골드 보상량 반환
    public long BaseRewardGold => baseRewardGold;

    // 기본 다이아 보상량 반환
    public long BaseRewardDiamond => baseRewardDiamond;

    // 기본 던전 마석 보상량 반환
    public long BaseRewardDungeonStone => baseRewardStageStone;
    public long BaseRewardStageStone => baseRewardStageStone; // 기존 호환용

    #endregion

    #region 보상 계산 메서드

    // 초과 전투력 기반 보너스 배율 계산
    public float CalculateBonusRatio(int currentTotalPower)
    {
        if (currentTotalPower < RequiredMinCombatPower)
        {
            return 0.0f;
        }

        return (float)(currentTotalPower - RequiredMinCombatPower) / RequiredMinCombatPower;
    }

    // 초과 보너스 적용 최종 골드 보상 계산
    public long CalculateFinalGold(int currentTotalPower)
    {
        float bonus = CalculateBonusRatio(currentTotalPower);
        return (long)Math.Floor(baseRewardGold * (1.0f + bonus));
    }

    // 초과 보너스 적용 최종 다이아 보상 계산
    public long CalculateFinalDiamond(int currentTotalPower)
    {
        float bonus = CalculateBonusRatio(currentTotalPower);
        return (long)Math.Floor(baseRewardDiamond * (1.0f + bonus));
    }

    // 초과 보너스 적용 최종 던전 마석 보상 계산
    public long CalculateFinalDungeonStone(int currentTotalPower)
    {
        float bonus = CalculateBonusRatio(currentTotalPower);
        return (long)Math.Floor(baseRewardStageStone * (1.0f + bonus));
    }

    public long CalculateFinalStageStone(int currentTotalPower) => CalculateFinalDungeonStone(currentTotalPower);

    #endregion
}
