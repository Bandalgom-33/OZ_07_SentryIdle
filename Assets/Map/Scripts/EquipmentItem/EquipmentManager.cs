using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 캐릭터별 4부위(머리, 갑옷, 무기, 장신구) 장비 장착 관리, 스탯 합산 및 세이브/로드 연동 싱글톤 매니저
public class EquipmentManager : SingletonBase<EquipmentManager>
{
    #region 인스펙터 바인딩 필드

    [Header("장비 슬롯")]
    [SerializeField] private EquipmentSlotUI headSlot;
    [SerializeField] private EquipmentSlotUI armorSlot;
    [SerializeField] private EquipmentSlotUI weaponSlot;
    [SerializeField] private EquipmentSlotUI accessorySlot;

    [Header("캐릭터 카드 UI")]
    [Tooltip("현재 선택된 캐릭터의 카드/초상화 이미지")]
    [SerializeField] private Image characterCardImage;

    [Tooltip("현재 선택된 캐릭터의 이름 텍스트")]
    [SerializeField] private TMP_Text characterNameText;

    [Tooltip("유닛 초상화 카탈로그 데이터")]
    [SerializeField] private UnitPortraitCatalogSO portraitCatalog;

    #endregion

    #region 내부 변수 및 프로퍼티

    // 현재 장착 중인 아이템
    private ItemDataSO equippedHead;
    private ItemDataSO equippedArmor;
    private ItemDataSO equippedWeapon;
    private ItemDataSO equippedAccessory;
    public EquipmentBonusStats CurrentBonusStats { get; private set; }

    public event Action<EquipmentBonusStats> OnEquipmentStatsChanged;

    public ItemDataSO EquippedHead => equippedHead;
    public ItemDataSO EquippedArmor => equippedArmor;
    public ItemDataSO EquippedWeapon => equippedWeapon;
    public ItemDataSO EquippedAccessory => equippedAccessory;
    public string CurrentUnitId => currentUnitId;

    private readonly Dictionary<string, CharacterEquipmentData> characterEquipments = new Dictionary<string, CharacterEquipmentData>();
    private string currentUnitId = "UNIT_0002"; // 기본값: 루카

    #endregion

    #region 라이프사이클 및 이벤트 구독

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DataSaveEvent>(OnSave);
        EventBus.Subscribe<DataLoadEvent>(OnLoad);
        EventBus.Subscribe<DataResetEvent>(OnReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DataSaveEvent>(OnSave);
        EventBus.Unsubscribe<DataLoadEvent>(OnLoad);
        EventBus.Unsubscribe<DataResetEvent>(OnReset);
    }

    #endregion
    

    public void TestSelectLuka()
    {
        SetCurrentUnit("UNIT_0002");
    }

    public void TestSelectKimHajin()
    {
        SetCurrentUnit("UNIT_0004");
    }
    
    
    
    public void EquipItem(ItemDataSO itemData)
    {
        if (itemData == null) return;
        if (string.IsNullOrEmpty(currentUnitId)) return;
        //현재 캐릭터 데이터 가져오기
        if (!characterEquipments.TryGetValue(currentUnitId, out CharacterEquipmentData characterData)) return;
        
        ItemDataSO previousItem = null;

        switch (itemData.EquipmentType)
        {
            case EquipmentType.Head:
                previousItem = equippedHead;
                equippedHead = itemData;
                characterData.Head = itemData;
                if (headSlot != null) headSlot.SetItem(itemData);
                break;

            case EquipmentType.Armor:
                previousItem = equippedArmor;
                equippedArmor = itemData;
                characterData.Armor = itemData;
                if (armorSlot != null) armorSlot.SetItem(itemData);
                break;

            case EquipmentType.Weapon:
                previousItem = equippedWeapon;
                equippedWeapon = itemData;
                characterData.Weapon = itemData;
                if (weaponSlot != null) weaponSlot.SetItem(itemData);
                break;

            case EquipmentType.Accessory:
                previousItem = equippedAccessory;
                equippedAccessory = itemData;
                characterData.Accessory = itemData;
                if (accessorySlot != null) accessorySlot.SetItem(itemData);
                break;
        }

        if (previousItem != null)
        {
            Debug.Log(
                $"{previousItem.ItemName} → {itemData.ItemName} 으로 교체"
            );
        }
        else
        {
            Debug.Log($"{itemData.ItemName} 장착");
        }
        RecalculateEquipmentStats();
        SaveManager.Instance?.SaveGameData();
    }
    
    public void UnequipItem(EquipmentType equipmentType)
    {
        if (string.IsNullOrEmpty(currentUnitId)) return;

        if (!characterEquipments.TryGetValue( currentUnitId, out CharacterEquipmentData characterData)) return;
        
        switch (equipmentType)
        {
            case EquipmentType.Head:
                equippedHead = null;
                characterData.Head = null;
                if (headSlot != null) headSlot.SetItem(null);
                break;

            case EquipmentType.Armor:
                equippedArmor = null;
                characterData.Armor = null;
                if (armorSlot != null) armorSlot.SetItem(null);
                break;

            case EquipmentType.Weapon:
                equippedWeapon = null;
                characterData.Weapon = null;
                if (weaponSlot != null) weaponSlot.SetItem(null);
                break;

            case EquipmentType.Accessory:
                equippedAccessory = null;
                characterData.Accessory = null;
                if (accessorySlot != null) accessorySlot.SetItem(null);
                break;
        }
        Debug.Log($"{equipmentType} 장비 해제");
        RecalculateEquipmentStats();
        SaveManager.Instance?.SaveGameData();
    }
    
    //장비스텟 계산하는 역할
    private void RecalculateEquipmentStats()
    {
        int physicalAttack = 0;
        int magicAttack = 0;

        int physicalDefense = 0;
        int magicDefense = 0;

        float criticalDamageBonus = 0f;
        float accuracy = 0f;

        AddItemStats(equippedHead);
        AddItemStats(equippedArmor);
        AddItemStats(equippedWeapon);
        AddItemStats(equippedAccessory);

        CurrentBonusStats = new EquipmentBonusStats(
            physicalAttack,
            magicAttack,
            physicalDefense,
            magicDefense,
            criticalDamageBonus,
            accuracy
        );

        OnEquipmentStatsChanged?.Invoke(CurrentBonusStats);

        void AddItemStats(ItemDataSO itemData)
        {
            if (itemData == null) return;

            physicalAttack += itemData.PhysicalAttack;
            magicAttack += itemData.MagicAttack;

            physicalDefense += itemData.PhysicalDefense;
            magicDefense += itemData.MagicDefense;

            criticalDamageBonus += itemData.CriticalDamageBonus;
            accuracy += itemData.Accuracy;
        }
    }
    
    //현재 어떤 캐릭터의 장비창을 보고 있는지?
    public void SetCurrentUnit(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return;

        currentUnitId = unitId;

        if (!characterEquipments.ContainsKey(unitId))
        {
            characterEquipments.Add(
                unitId,
                new CharacterEquipmentData(unitId)
            );
        }

        LoadCurrentUnitEquipment();
    }

    // 슬롯 UI 인스펙터/런타임 바인딩 헬퍼
    public void BindSlotUIs(EquipmentSlotUI head, EquipmentSlotUI armor, EquipmentSlotUI weapon, EquipmentSlotUI accessory)
    {
        headSlot = head;
        armorSlot = armor;
        weaponSlot = weapon;
        accessorySlot = accessory;
        LoadCurrentUnitEquipment();
    }
    
    //장비 데이터를 불러오는 역할
    public void LoadCurrentUnitEquipment()
    {
        if (string.IsNullOrEmpty(currentUnitId)) return;

        if (!characterEquipments.TryGetValue(
                currentUnitId,
                out CharacterEquipmentData data))
        {
            return;
        }

        equippedHead = data.Head;
        equippedArmor = data.Armor;
        equippedWeapon = data.Weapon;
        equippedAccessory = data.Accessory;

        if (headSlot != null) headSlot.SetItem(equippedHead);
        if (armorSlot != null) armorSlot.SetItem(equippedArmor);
        if (weaponSlot != null) weaponSlot.SetItem(equippedWeapon);
        if (accessorySlot != null) accessorySlot.SetItem(equippedAccessory);

        UpdateCharacterCardUI();
        RecalculateEquipmentStats();
    }

    // 현재 선택된 캐릭터의 카드 이미지 및 이름 텍스트 갱신 처리
    private void UpdateCharacterCardUI()
    {
        if (string.IsNullOrEmpty(currentUnitId)) return;

        if (portraitCatalog != null)
        {
            Sprite portrait = portraitCatalog.GetPortraitByUnitId(currentUnitId);
            if (characterCardImage != null)
            {
                characterCardImage.sprite = portrait;
                characterCardImage.enabled = portrait != null;
            }

            // 카탈로그에서 유닛 데이터를 조회하여 표시 이름 반영
            UnitDataSO unitData = portraitCatalog.GetUnitDataByUnitId(currentUnitId);
            if (characterNameText != null)
            {
                characterNameText.text = unitData != null ? unitData.DisplayName : currentUnitId;
            }
        }
        else
        {
            if (characterNameText != null)
            {
                characterNameText.text = currentUnitId;
            }
        }
    }

    #region 세이브 / 로드 연동

    // 장비 장착 세이브 데이터 저장 처리
    private void OnSave(DataSaveEvent evt)
    {
        if (evt.saveData == null) return;
        if (evt.saveData.equipment == null)
        {
            evt.saveData.equipment = new EquipmentSaveData();
        }

        evt.saveData.equipment.characterEquipments.Clear();
        foreach (var pair in characterEquipments)
        {
            if (pair.Value == null) continue;

            evt.saveData.equipment.characterEquipments.Add(new CharacterEquipmentSaveEntry
            {
                unitId = pair.Key,
                headItemId = pair.Value.Head != null ? pair.Value.Head.ItemID : string.Empty,
                armorItemId = pair.Value.Armor != null ? pair.Value.Armor.ItemID : string.Empty,
                weaponItemId = pair.Value.Weapon != null ? pair.Value.Weapon.ItemID : string.Empty,
                accessoryItemId = pair.Value.Accessory != null ? pair.Value.Accessory.ItemID : string.Empty
            });
        }
    }

    // 장비 장착 세이브 데이터 로드 처리
    private void OnLoad(DataLoadEvent evt)
    {
        if (evt.saveData == null || evt.saveData.equipment == null) return;

        characterEquipments.Clear();

        if (evt.saveData.equipment.characterEquipments != null)
        {
            InventoryGridManager inv = InventoryGridManager.Instance;
            for (int i = 0; i < evt.saveData.equipment.characterEquipments.Count; i++)
            {
                CharacterEquipmentSaveEntry entry = evt.saveData.equipment.characterEquipments[i];
                if (entry == null || string.IsNullOrEmpty(entry.unitId)) continue;

                CharacterEquipmentData data = new CharacterEquipmentData(entry.unitId);
                if (inv != null)
                {
                    if (!string.IsNullOrEmpty(entry.headItemId)) data.Head = inv.GetItemById(entry.headItemId);
                    if (!string.IsNullOrEmpty(entry.armorItemId)) data.Armor = inv.GetItemById(entry.armorItemId);
                    if (!string.IsNullOrEmpty(entry.weaponItemId)) data.Weapon = inv.GetItemById(entry.weaponItemId);
                    if (!string.IsNullOrEmpty(entry.accessoryItemId)) data.Accessory = inv.GetItemById(entry.accessoryItemId);
                }

                characterEquipments[entry.unitId] = data;
            }
        }

        LoadCurrentUnitEquipment();
    }

    // 장비 데이터 초기화 처리
    private void OnReset(DataResetEvent evt)
    {
        characterEquipments.Clear();
        equippedHead = null;
        equippedArmor = null;
        equippedWeapon = null;
        equippedAccessory = null;

        if (headSlot != null) headSlot.SetItem(null);
        if (armorSlot != null) armorSlot.SetItem(null);
        if (weaponSlot != null) weaponSlot.SetItem(null);
        if (accessorySlot != null) accessorySlot.SetItem(null);

        RecalculateEquipmentStats();
    }

    #endregion
}