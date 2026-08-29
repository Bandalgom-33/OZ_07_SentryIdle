using EndlessGuard.Unit.Data;

// 가챠 획득 결과의 세부 상태를 구분하는 열거형 (신규 해금, 한계돌파 상승, 풀돌 도달/유지)
public enum GachaResultType
{
    // 신규 미보유 캐릭터 최초 획득 (해금, 0돌파)
    NewUnlock,
    // 보유 캐릭터 중복 획득으로 인한 한계돌파 단계 상승 (1 ~ 6돌파)
    Breakthrough,
    // 이미 최대 한계돌파(6단계 풀돌)에 도달한 캐릭터 중복 획득
    MaxBreakthroughReached
}

// 가챠 추첨 보상 결과를 전달하는 경량 DTO 클래스
// 값 타입 구조체(struct) + 인터페이스(interface) 조합 시 발생하는 힙 박싱(GC Alloc)을 원천 차단하기 위해 순수 참조 클래스로 설계됨
public class GachaRewardItem
{
    // 대상 유닛 ScriptableObject 원본 데이터 참조
    public UnitDataSO TargetUnitData { get; private set; }

    // 유닛 고유 정수 ID (예: 2 -> UNIT_0002)
    public int UnitId { get; private set; }

    // 유닛 고유 식별자 문자열 (예: "UNIT_0002")
    public string RewardId => TargetUnitData != null ? TargetUnitData.UnitId : string.Empty;

    // 유닛 성 등급 (1성 ~ 6성)
    public UnitGrade Grade => TargetUnitData != null ? TargetUnitData.Grade : UnitGrade.None;

    // 화면 표시 이름 (예: "루카")
    public string DisplayName => TargetUnitData != null ? TargetUnitData.DisplayName : "Unknown";

    // 기존 보유 여부 플래그
    public bool IsOwned { get; set; }

    // 중복 획득 시 처리 결과 타입 (신규/돌파/풀돌)
    public GachaResultType ResultType { get; set; }

    // 돌파 적용 이전 단계 (0 ~ 6)
    public int PreviousBreakthroughStep { get; set; }

    // 돌파 적용 이후 현재 단계 (0 ~ 6)
    public int CurrentBreakthroughStep { get; set; }

    // 중복 획득 시 부여 조각 수량 (기본값: 10)
    public int DuplicatePieceAmount { get; private set; }

    // 가챠 보상 DTO 생성자
    public GachaRewardItem(UnitDataSO unitData, int unitId, bool isOwned = false, int duplicatePieceAmount = 10)
    {
        TargetUnitData = unitData;
        UnitId = unitId;
        IsOwned = isOwned;
        ResultType = isOwned ? GachaResultType.Breakthrough : GachaResultType.NewUnlock;
        PreviousBreakthroughStep = 0;
        CurrentBreakthroughStep = 0;
        DuplicatePieceAmount = duplicatePieceAmount;
    }
}
