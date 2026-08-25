using UnityEngine;

[System.Serializable]
public class StageTileSet
{
    [Header("스테이지 타일 프리팹")]
    public TileView groundTilePrefab;
    public TileView pathTilePrefab;
    public TileView highGroundTilePrefab;
    public TileView spawnTilePrefab;
    public TileView goalTilePrefab;
}