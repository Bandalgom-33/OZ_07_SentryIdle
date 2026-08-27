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
        if(node == null) return;

        Debug.Log(
           $"좌표: {node.GridPosition} / " +
           $"타입: {node.TileType} / " +
           $"이동 가능: {node.IsWalkable} / " +
           $"배치 가능: {node.IsDeployable}"
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

            CreateVisual(floorEntry);

            TileVisualEntry obstacleEntry = visualSet.GetRandomVisual(TileType.Obstacle);

            CreateVisual(obstacleEntry);

            return;
        }

        // 그 외 타일은 기존처럼 비주얼 하나만 생성
        TileVisualEntry visualEntry = visualSet.GetRandomVisual(node.TileType);

        currentVisual = CreateVisual(visualEntry);
    }
    
    
    //에셋 프리팹 생성 위치 보정하기
    private GameObject CreateVisual(TileVisualEntry entry)
    {
        if (entry == null) return null;
        if (entry.prefab == null) return null;
        if (visualRoot == null) return null;

        GameObject visual = Instantiate( entry.prefab, visualRoot );

        visual.transform.localPosition = new Vector3(0f, visualYOffset, 0f) + entry.positionOffset;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = entry.scale;

        return visual;
    }

}
