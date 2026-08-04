using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Grid Å©±â")]
    [SerializeField, Min(1)] private int width = 12;
    [SerializeField, Min(1)] private int height = 8;

    [Header("¸Ê Å©±â")]
    [SerializeField] private GridMapRenderer mapRenderer;

    private TileNode[,] grid;

    public TileNode[,] Grid => grid;
    public int Width => width;
    public int Height => height;



    void Start()
    {
        GenerateMap();
    }


    public void GenerateMap()
    {
        InitializedGrid();

        if (mapRenderer == null) return;
        mapRenderer.RenderMap(grid);
    }


    public void InitializedGrid()
    {
        grid = new TileNode[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                grid[x, y] = new TileNode(gridPosition);
            }
        }
    }

}
