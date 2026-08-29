using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인게임 전투 화면 포션 순환 및 사용 컨트롤러
public class InGamePotionUI : MonoBehaviour
{
    #region 인스펙터 바인딩 필드

    [Header("--- 필수 UI 요소 ---")]
    [Tooltip("현재 선택된 포션의 아이콘 이미지")]
    [SerializeField] private Image potionIconImage;

    [Tooltip("포션 종류를 순환 변경하는 버튼")]
    [SerializeField] private Button changePotionButton;

    [Tooltip("현재 선택된 포션을 사용하는 버튼")]
    [SerializeField] private Button usePotionButton;

    [Header("--- 선택적 UI 요소 ---")]
    [Tooltip("현재 선택된 포션의 보유 수량 표시 텍스트")]
    [SerializeField] private TMP_Text potionCountText;

    [Tooltip("포션 쿨타임 진행도를 표시하는 슬라이더 또는 Fill Image")]
    [SerializeField] private Image cooldownOverlayImage;

    [Tooltip("포션 쿨타임 남은 시간 텍스트")]
    [SerializeField] private TMP_Text cooldownText;

    #endregion

    #region 내부 변수

    // 순환 가능한 3종 체력 포션 타입 목록
    private readonly ConsumableType[] _availablePotionTypes = new ConsumableType[]
    {
        ConsumableType.HealthPotion_Low,
        ConsumableType.HealthPotion_Mid,
        ConsumableType.HealthPotion_High
    };

    private int _selectedPotionIndex = 0;

    public ConsumableType CurrentSelectedPotion => _availablePotionTypes[_selectedPotionIndex];

    #endregion

    #region 라이프사이클 및 이벤트 바인딩

    private void Awake()
    {
        if (changePotionButton != null)
        {
            changePotionButton.onClick.AddListener(OnClickChangePotion);
        }

        if (usePotionButton != null)
        {
            usePotionButton.onClick.AddListener(OnClickUsePotion);
        }
    }

    private void OnEnable()
    {
        SelectHighestPriorityAvailablePotion();

        ConsumableItemManager.OnConsumableCountChanged += HandleConsumableCountChanged;
        if (InventoryGridManager.Instance != null)
        {
            InventoryGridManager.Instance.OnInventoryChanged += RefreshCurrentPotionUI;
        }

        RefreshCurrentPotionUI();
    }

    private void OnDisable()
    {
        ConsumableItemManager.OnConsumableCountChanged -= HandleConsumableCountChanged;
        if (InventoryGridManager.Instance != null)
        {
            InventoryGridManager.Instance.OnInventoryChanged -= RefreshCurrentPotionUI;
        }
    }

    private void Update()
    {
        UpdateCooldownDisplay();
    }

    // 보유 중인 최우선 순위(하급 > 중급 > 상급) 포션 자동 선택
    private void SelectHighestPriorityAvailablePotion()
    {
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cim == null) return;

        for (int i = 0; i < _availablePotionTypes.Length; i++)
        {
            if (cim.GetItemCount(_availablePotionTypes[i]) > 0)
            {
                _selectedPotionIndex = i;
                return;
            }
        }

        _selectedPotionIndex = 0;
    }

    // 미보유 포션 선택 시 보유 포션으로 자동 인덱스 보정
    private void AutoSelectAvailablePotion()
    {
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cim == null) return;

        if (cim.GetItemCount(CurrentSelectedPotion) <= 0)
        {
            SelectHighestPriorityAvailablePotion();
        }
    }

    #endregion

    #region 포션 UI 갱신 및 쿨타임 처리

    // 현재 선택된 포션 UI 상태 갱신
    public void RefreshCurrentPotionUI()
    {
        AutoSelectAvailablePotion();

        ConsumableType currentType = CurrentSelectedPotion;

        ItemDataSO itemData = InventoryGridManager.Instance != null
            ? InventoryGridManager.Instance.GetConsumableItemData(currentType)
            : null;

        if (potionIconImage != null)
        {
            if (itemData != null && itemData.ItemIcon != null)
            {
                potionIconImage.sprite = itemData.ItemIcon;
                potionIconImage.enabled = true;
            }
            else
            {
                potionIconImage.enabled = false;
            }
        }

        int currentCount = ConsumableItemManager.Instance != null
            ? ConsumableItemManager.Instance.GetItemCount(currentType)
            : 0;

        if (potionCountText != null)
        {
            potionCountText.text = $"{currentCount:#,##0}";
        }

        UpdateUseButtonInteractable(currentCount);
    }

    // 포션 쿨타임 오버레이 및 텍스트 갱신
    private void UpdateCooldownDisplay()
    {
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cim == null) return;

        bool isReady = cim.IsPotionReady;
        float remaining = cim.PotionRemainingCooldown;
        const float maxCooldown = 5.0f;

        if (cooldownOverlayImage != null)
        {
            cooldownOverlayImage.enabled = !isReady;
            cooldownOverlayImage.fillAmount = Mathf.Clamp01(remaining / maxCooldown);
        }

        if (cooldownText != null)
        {
            if (!isReady)
            {
                cooldownText.text = $"{remaining:F1}s";
                cooldownText.enabled = true;
            }
            else
            {
                cooldownText.text = string.Empty;
                cooldownText.enabled = false;
            }
        }

        int currentCount = cim.GetItemCount(CurrentSelectedPotion);
        UpdateUseButtonInteractable(currentCount);
    }

    // 포션 사용 버튼 상호작용 상태 갱신
    private void UpdateUseButtonInteractable(int currentCount)
    {
        if (usePotionButton == null) return;

        ConsumableItemManager cim = ConsumableItemManager.Instance;
        bool isReady = cim != null && cim.IsPotionReady;

        usePotionButton.interactable = (currentCount > 0 && isReady);
    }

    // 소모품 수량 변경 이벤트 핸들러
    private void HandleConsumableCountChanged(ConsumableType type, int newCount)
    {
        RefreshCurrentPotionUI();
    }

    #endregion

    #region 사용자 조작 버튼 핸들러

    // 보유 포션 순환 변경 버튼 클릭 처리
    public void OnClickChangePotion()
    {
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cim == null) return;

        int totalTypes = _availablePotionTypes.Length;
        int nextIndex = -1;

        for (int step = 1; step <= totalTypes; step++)
        {
            int candidateIndex = (_selectedPotionIndex + step) % totalTypes;
            ConsumableType candidateType = _availablePotionTypes[candidateIndex];

            if (cim.GetItemCount(candidateType) > 0)
            {
                nextIndex = candidateIndex;
                break;
            }
        }

        if (nextIndex != -1 && nextIndex != _selectedPotionIndex)
        {
            _selectedPotionIndex = nextIndex;
            RefreshCurrentPotionUI();
            Debug.Log($"[InGamePotionUI] 보유 포션으로 변경 완료 -> {CurrentSelectedPotion} (보유: {cim.GetItemCount(CurrentSelectedPotion)}개)");
        }
        else if (nextIndex == _selectedPotionIndex)
        {
            Debug.Log($"[InGamePotionUI] 현재 보유 중인 포션은 {CurrentSelectedPotion} 1종류뿐입니다.");
        }
        else
        {
            Debug.LogWarning("[InGamePotionUI] 현재 인벤토리에 보유 중인 포션이 없습니다.");
        }
    }

    // 포션 사용 버튼 클릭 처리
    public void OnClickUsePotion()
    {
        ConsumableItemManager cim = ConsumableItemManager.Instance;
        if (cim == null) return;

        ConsumableType selectedType = CurrentSelectedPotion;

        bool used = cim.UseHealthPotion(selectedType);
        if (used)
        {
            Debug.Log($"[InGamePotionUI] {selectedType} 사용 성공! 필드 아군 유닛 체력 회복 완료.");
            RefreshCurrentPotionUI();
        }
        else
        {
            Debug.LogWarning($"[InGamePotionUI] {selectedType} 사용 실패 (수량 부족 또는 쿨타임 중)");
        }
    }

    #endregion
}