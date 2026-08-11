using UnityEngine;

public enum TileType
{
    Empty, //역할 미정
    Ground,// 일반 지상, 근거리 배치가능타일
    Path, // 적 이동 경로
    HighGround, // 원거리 캐릭터용 고지대
    Obstacle, // 이동 배치가 불가능한 타일
    Spawn,// 적 출발 타일
    Goal// 적 도착 타일
}
