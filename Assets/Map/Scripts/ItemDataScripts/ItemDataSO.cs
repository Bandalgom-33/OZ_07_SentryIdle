using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Item/ItemData")]

public class ItemDataSO : ScriptableObject
{

    [Header("기본 정보")]
    //아이템 구분을 위한 고유 ID -> Weapon_iron_sword 등등
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;

    [Header("아이템 분류")]
    [SerializeField] private ItemCategory itemCategory;

    //보유 가능한 최대 아이템 갯수
    [Header("중첩")] 
    [SerializeField] private int maxStack = 999;
    
    [Header("장비 정보")]
    [SerializeField] private EquipmentType equipmentType;
    

    public string ItemID =>  itemId;
    public string ItemName =>  itemName;
    public Sprite ItemIcon => itemIcon;
    public ItemCategory ItemCategory => itemCategory;
    public int MaxStack => maxStack;
    public  EquipmentType EquipmentType => equipmentType;
}
