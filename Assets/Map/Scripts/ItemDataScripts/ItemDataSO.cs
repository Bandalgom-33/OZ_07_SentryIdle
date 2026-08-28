using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Item/ItemData")]
public class ItemDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;

    [TextArea(2, 5)]
    [SerializeField] private string description;
    
    //아이템 구분을 위한 고유 ID -> Weapon_iron_sword 등등
    [Header("아이템 분류")]
    [SerializeField] private ItemCategory itemCategory;

    //보유 가능한 최대 아이템 갯수
    [Header("중첩")]
    [SerializeField] private int maxStack = 999;

    [Header("장비 정보")]
    [SerializeField] private EquipmentType equipmentType;

    [Header("소모품 정보")]
    [SerializeField] private ConsumableType consumableType = ConsumableType.HealthPotion_Low;
    [Tooltip("물약 회복 비율")]
    [SerializeField, Range(0f, 1f)] private float recoveryRatio = 0.25f;
    [Tooltip("경험치책 경험치량")]
    [SerializeField, Min(0)] private long expAmount = 100L;

    [Header("장비 능력치")]
    [SerializeField] private int physicalAttack;
    [SerializeField] private int magicAttack;

    [SerializeField] private int physicalDefense;
    [SerializeField] private int magicDefense;

    [SerializeField] private float criticalDamageBonus;
    [SerializeField] private float accuracy;

    public string ItemID => itemId;
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public string Description => description;

    public ItemCategory ItemCategory => itemCategory;
    public int MaxStack => maxStack;
    public EquipmentType EquipmentType => equipmentType;
    public ConsumableType ConsumableType => consumableType;
    public float RecoveryRatio => recoveryRatio;
    public long ExpAmount => expAmount;

    public int PhysicalAttack => physicalAttack;
    public int MagicAttack => magicAttack;

    public int PhysicalDefense => physicalDefense;
    public int MagicDefense => magicDefense;

    public float CriticalDamageBonus => criticalDamageBonus;
    public float Accuracy => accuracy;
}