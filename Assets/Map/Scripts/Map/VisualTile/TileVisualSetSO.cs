using UnityEngine;

[CreateAssetMenu(
    fileName = "NewTileVisualSet",
    menuName = "Map/Tile Visual Set"
)]
public class TileVisualSetSO : ScriptableObject
{
    [Header("기본 바닥")]
    [SerializeField] private TileVisualEntry[] floorVisuals;

    [Header("High Ground / 경사")]
    [SerializeField] private TileVisualEntry[] highGroundVisuals;

    [Header("Obstacle / 장식")]
    [SerializeField] private TileVisualEntry[] obstacleVisuals;

    [Header("Spawn / 입구")]
    [SerializeField] private TileVisualEntry[] spawnVisuals;

    [Header("Goal / 출구")]
    [SerializeField] private TileVisualEntry[] goalVisuals;
    
    [Header("Path / 적 이동 경로")]
    [SerializeField] private TileVisualEntry[] pathVisuals;

    public TileVisualEntry GetRandomVisual(TileType tileType)
    {
        TileVisualEntry[] visuals = null;

        switch (tileType)
        {
            case TileType.Ground:
                visuals = floorVisuals;
                break;

            case TileType.Path:
                visuals = pathVisuals;
                break;
            
            case TileType.HighGround:
                visuals = highGroundVisuals;
                break;

            case TileType.Obstacle:
                visuals = obstacleVisuals;
                break;

            case TileType.Spawn:
                visuals = spawnVisuals;
                break;

            case TileType.Goal:
                visuals = goalVisuals;
                break;

            default:
                return null;
        }

        if (visuals == null || visuals.Length == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, visuals.Length);

        return visuals[randomIndex];
    }
    
    public TileVisualEntry GetRandomFloorVisual()
    {
        if (floorVisuals == null || floorVisuals.Length == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, floorVisuals.Length);

        return floorVisuals[randomIndex];
    }
}