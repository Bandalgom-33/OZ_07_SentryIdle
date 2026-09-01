using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 편성 팝업 상단 개별 선택 유닛 슬롯 UI 제어 컴포넌트
public class UIDungeonSelectedUnitSlot : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 중첩 프리팹 컴포넌트 ---")]
    [Tooltip("슬롯 내부의 캐릭터 이미지 카드 제어 컴포넌트")]
    [SerializeField] private UIDungeonCharacterImageCard characterImageCard;

    [Tooltip("슬롯 해제 [X] 버튼 (부정 버튼)")]
    [SerializeField] private Button removeButton;

    [Header("--- 유닛 상세 정보 텍스트 ---")]
    [Tooltip("유닛 이름 텍스트")]
    [SerializeField] private TMP_Text unitNameText;

    [Tooltip("유닛 레벨 텍스트")]
    [SerializeField] private TMP_Text unitLevelText;

    [Tooltip("유닛 돌파/등급 텍스트")]
    [SerializeField] private TMP_Text unitGradeText;

    [Tooltip("유닛 전투력 텍스트")]
    [SerializeField] private TMP_Text combatPowerText;

    #endregion

    #region 내부 상태 필드

    private int _slotIndex = -1;
    private int _unitId = -1;
    private Action<int> _onRemoveCallback;

    #endregion

    #region 유니티 생명주기 및 리스너 등록

    // 컴포넌트 초기화 및 해제 버튼 리스너 등록
    private void Awake()
    {
        if (characterImageCard == null)
        {
            characterImageCard = GetComponentInChildren<UIDungeonCharacterImageCard>(true);
        }

        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnClickRemove);
        }
    }

    // 버튼 이벤트 리스너 해제
    private void OnDestroy()
    {
        if (removeButton != null)
        {
            removeButton.onClick.RemoveListener(OnClickRemove);
        }
    }

    // 슬롯 유닛 해제 버튼 클릭 이벤트 처리
    private void OnClickRemove()
    {
        if (_slotIndex >= 0)
        {
            _onRemoveCallback?.Invoke(_slotIndex);
        }
    }

    #endregion

    #region 데이터 바인딩 및 상태 제어

    // 유닛 데이터 및 초상화 스프라이트 바인딩 처리
    public void Bind(
        int slotIndex,
        int unitId,
        string unitName,
        int level,
        int starGrade,
        int combatPower,
        Sprite portraitSprite,
        Action<int> onRemove)
    {
        _slotIndex = slotIndex;
        _unitId = unitId;
        _onRemoveCallback = onRemove;

        if (characterImageCard != null)
        {
            characterImageCard.SetCharacter(portraitSprite);
        }

        if (unitNameText != null)
        {
            unitNameText.text = unitName;
        }

        if (unitLevelText != null)
        {
            unitLevelText.text = $"Lv.{level}";
        }

        if (unitGradeText != null)
        {
            unitGradeText.text = $"{starGrade}성";
        }

        if (combatPowerText != null)
        {
            combatPowerText.text = $"전투력 {combatPower}";
        }

        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(true);
        }
    }

    // 슬롯 초기화 및 빈 슬롯 전환 처리
    public void SetEmpty(int slotIndex, Action<int> onRemove = null)
    {
        _slotIndex = slotIndex;
        _unitId = -1;
        _onRemoveCallback = onRemove;

        if (characterImageCard != null)
        {
            characterImageCard.SetEmpty();
        }

        if (unitNameText != null)
        {
            unitNameText.text = "미배치";
        }

        if (unitLevelText != null)
        {
            unitLevelText.text = "-";
        }

        if (unitGradeText != null)
        {
            unitGradeText.text = "-";
        }

        if (combatPowerText != null)
        {
            combatPowerText.text = "전투력 0";
        }

        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(false);
        }
    }

    // 슬롯 선택 하이라이트 상태 설정
    public void SetSelected(bool isSelected)
    {
        if (characterImageCard != null)
        {
            characterImageCard.SetSelected(isSelected);
        }
    }

    #endregion
}
