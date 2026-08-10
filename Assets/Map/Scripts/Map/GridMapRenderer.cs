using UnityEngine;

public class GridMapRenderer : MonoBehaviour
{
    [Header("鸥老 汲沥")]
    [SerializeField] private TileView tilePrefab;

    [SerializeField, Min(0.1f)]
    private float tileSize = 1.0f;

    [Header("积己 鸥老 何葛")]
    [SerializeField] private Transform tileRoot;

    public void RenderMap(TileNode[,] grid)
    {
        if (grid == null) return;
        if(tilePrefab == null) return;
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                CreateTileView(grid[x,y]);
            }
        }
    }

    private void CreateTileView(TileNode node)
    {
        Vector3 worldPosition = GridToWorld(node.GridPosition);

        TileView tileView = Instantiate(tilePrefab, worldPosition, Quaternion.identity, tileRoot);

        tileView.Initialize(node);
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * tileSize, 0f, gridPosition.y * tileSize);
    }

}
