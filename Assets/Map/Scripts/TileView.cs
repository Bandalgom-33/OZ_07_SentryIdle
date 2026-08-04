using UnityEngine;

public class TileView : MonoBehaviour
{
    private TileNode node;

    public TileNode Node => node;

    public void Initialize(TileNode tilenode)
    {
        if (tilenode == null)
        {
            Debug.Log("TileView에 절달된 NODE가 비어있음 확인하셈");
            return;
        }

        node = tilenode;

        //하이라키에서 좌표 확인을 쉽게 이름설정하기
        gameObject.name = $"Tile_{node.GridPosition.x}_{node.GridPosition.y}"; 
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

}
