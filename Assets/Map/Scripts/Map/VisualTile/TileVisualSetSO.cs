using UnityEngine;

[CreateAssetMenu(
    fileName = "NewTileVisualSet",
    menuName = "Map/Tile Visual Set"
)]
public class TileVisualSetSO : ScriptableObject
{
    [Header("기본 바닥")]
    [SerializeField] private GameObject[] floorVisuals;

    [Header("High Ground / 경사")]
    [SerializeField] private GameObject[] highGroundVisuals;

    [Header("Obstacle / 장식")]
    [SerializeField] private GameObject[] obstacleVisuals;

    [Header("Spawn / 입구")]
    [SerializeField] private GameObject[] spawnVisuals;

    [Header("Goal / 출구")]
    [SerializeField] private GameObject[] goalVisuals;

    public GameObject GetRandomVisual(TileType tileType)
    {
        GameObject[] visuals = null;

        switch (tileType)
        {
            case TileType.Ground:
            case TileType.Path:
                visuals = floorVisuals;
                break;
            
            case TileType.HighGround:
                visuals = floorVisuals;
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
}