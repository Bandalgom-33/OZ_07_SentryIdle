using UnityEngine;

public class TileView : MonoBehaviour
{
    [Header("타일 타입별 Materaial")]
    [SerializeField] private Material emptyMaterial;
    [SerializeField] private Material pathMaterial;
    [SerializeField] private Material spawnMaterial;
    [SerializeField] private Material goalMaterial;
    [SerializeField] private Material groundMaterial;
    [SerializeField] private Material highGroundMaterial;
    
    [Header("타일 비주얼 위치 보정")]
    [SerializeField] private float visualYOffset = -0.8f;


    [Header("타일 높이 설정")]
    [SerializeField] private float highGroundHeight = 0.1f;
    
    [Header("타일 Visual")]
    [SerializeField] private Transform visualRoot;

[Header("타일 비주얼 크기 보정")]
[SerializeField] private Vector3 visualScale = Vector3.one;

    private GameObject currentVisual;

    private TileNode node;
    private MeshRenderer meshRenderer;

   public TileNode Node => node;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Initialize(TileNode tilenode,TileVisualSetSO visualSet)
    {
        if (tilenode == null) return;
        

        node = tilenode;

        //하이라키에서 좌표 확인을 쉽게 이름설정하기
        gameObject.name = $"Tile_{node.GridPosition.x}_{node.GridPosition.y}";

        ApplyMaterial();
        ApplyHeight();
        
        ApplyVisual(visualSet);
    }

   
    private void OnMouseDown()
    {
        if (node == null) return;

        MapGenerator mapGenerator =
            FindFirstObjectByType<MapGenerator>();

        bool inPathA =
            mapGenerator != null &&
            mapGenerator.IsInPathA(node.GridPosition);

        bool inPathB =
            mapGenerator != null &&
            mapGenerator.IsInPathB(node.GridPosition);

        Debug.Log(
            $"좌표: {node.GridPosition} / " +
            $"타입: {node.TileType} / " +
            $"PathA: {inPathA} / " +
            $"PathB: {inPathB}"
        );
    }
    

    private void ApplyMaterial()
    {
        if(meshRenderer == null) return;

        switch (node.TileType)
        {
            case TileType.Path:
                meshRenderer.material = pathMaterial;
                break;

            case TileType.Spawn:
                meshRenderer.material = spawnMaterial;
                break;

            case TileType.Goal:
                meshRenderer.material = goalMaterial;
                break;

            case TileType.Ground:
                meshRenderer.material = groundMaterial;
                break;

            case TileType.HighGround:
                meshRenderer.material = highGroundMaterial;
                break;

            default:
                meshRenderer.material = emptyMaterial;
                break;
        }
    }

    //배치 가능 타일 높이 조절하기
    private void ApplyHeight()
    {
        if (node.TileType != TileType.HighGround)
            return;

        Vector3 position = transform.position;

        position.y += highGroundHeight;

        transform.position = position;

        Debug.Log(
            $"[HighGround] {gameObject.name} / " +
            $"Height: {highGroundHeight} / " +
            $"World Y: {transform.position.y}"
        );
    }
    
    private void ApplyVisual(TileVisualSetSO visualSet)
    {
        if (visualSet == null) return;
        if (visualRoot == null) return;

        // Obstacle 타일은
        // 1. 기본 바닥 생성
        // 2. 그 위에 장애물 비주얼 추가 생성
        if (node.TileType == TileType.Obstacle)
        {
            TileVisualEntry floorEntry = visualSet.GetRandomFloorVisual();

            CreateVisual( floorEntry, true);

            TileVisualEntry obstacleEntry = visualSet.GetRandomVisual(TileType.Obstacle);

            CreateVisual( obstacleEntry, false);

            return;
        }

        // 그 외 타일은 기존처럼 비주얼 하나만 생성
        TileVisualEntry visualEntry = visualSet.GetRandomVisual(node.TileType);
        
        if (visualEntry != null && visualEntry.prefab != null)
        {
            Debug.Log(
                $"[Tile Visual] {gameObject.name} / " +
                $"Type: {node.TileType} / " +
                $"Prefab: {visualEntry.prefab.name}"
            );
        }

        currentVisual = CreateVisual(visualEntry, true );
    }
    
    
    //에셋 프리팹 생성 위치 보정하기
    private GameObject CreateVisual( TileVisualEntry entry, bool fitToTile )
    {
        if (entry == null) return null;
        if (entry.prefab == null) return null;
        if (visualRoot == null) return null;

        GameObject visual = Instantiate(entry.prefab, visualRoot );
        visual.transform.localPosition = new Vector3(0f, visualYOffset, 0f) + entry.positionOffset;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = entry.scale;

        if (fitToTile)
        {
            FitVisualToTile(visual);
        }

        return visual;
    }
    
    
    private void FitVisualToTile(GameObject visual)
    {
        if (visual == null) return;
    
        Renderer[] renderers =
            visual.GetComponentsInChildren<Renderer>();
    
        if (renderers == null || renderers.Length == 0)
            return;
    
        Bounds bounds = renderers[0].bounds;
    
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
    
        float width = bounds.size.x;
        float depth = bounds.size.z;
    
        if (width <= 0f || depth <= 0f)
            return;
    
        // 한 칸보다 살짝 작게 만들어서 옆 타일과 겹치지 않도록 함
        const float targetSize = 0.95f;
    
        float scaleFactor =
            Mathf.Min(
                targetSize / width,
                targetSize / depth
            );
    
        visual.transform.localScale *= scaleFactor;
    }

}
