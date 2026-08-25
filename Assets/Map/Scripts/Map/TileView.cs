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


    [Header("타일 높이 설정")]
    [SerializeField] private float highGroundHeight = 0.25f;
    
    [Header("타일 Visual")]
    [SerializeField] private Transform visualRoot;

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

   /*
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
    */

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
        Vector3 position = transform.position;

        if(node.TileType == TileType.HighGround)
        {
            position.y = highGroundHeight;
        }
        else
        {
            position.y = 0f;
        }
        transform.position = position;  
    }
    
    private void ApplyVisual(TileVisualSetSO visualSet)
    {
        if (visualSet == null) return;
        if (visualRoot == null) return;

        GameObject visualPrefab =
            visualSet.GetRandomVisual(node.TileType);

        if (visualPrefab == null) return;

        currentVisual = Instantiate(
            visualPrefab,
            visualRoot
        );

        currentVisual.transform.localPosition = Vector3.zero;
        currentVisual.transform.localRotation = Quaternion.identity;
    }

}
