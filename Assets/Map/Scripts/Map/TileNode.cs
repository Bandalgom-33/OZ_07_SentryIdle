using UnityEngine;

public class TileNode
{
    //Grid 배열상 논리 좌표
    public Vector2Int GridPosition {  get; private set; }

    //현재 타일의 종류
    public TileType TileType { get; private set; }

    //적이 이동할 수 있는 타일인지 확인
    public bool IsWalkable {  get; private set; }

    //캐릭터를 배치할 수 있는 타일인지 확인
    public bool IsDeployable {  get; private set; }

    //현재 타일이 사용 중인지 확인
    public bool IsOccupied { get; private set; }

    public TileNode(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;

        IsOccupied = false;

        //처음 생성될 때는 Empty 상태로 고정
        SetTileType(TileType.Empty);
    }


    public void SetOccupied(bool isOccupied)
    {
        IsOccupied = isOccupied;
    }


    public void SetTileType(TileType tileType)
    {
        TileType = tileType;

        IsWalkable =
            tileType == TileType.Path ||
            tileType == TileType.Spawn ||
            tileType == TileType.Goal;

        IsDeployable =
            tileType == TileType.Path ||
            tileType == TileType.HighGround;
    }
}
