using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전 유닛 편성 팝업 유닛 카드 UI 컴포넌트
public class UIDungeonFormationUnitCard : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 핵심 UI 구성 요소 (버튼 형태) ---")]
    [Tooltip("카드 클릭 선택 버튼 (루트 Button 컴포넌트)")]
    [SerializeField] private Button cardButton;

    [Tooltip("유닛 초상화 이미지")]
    [SerializeField] private Image portraitImage;

    [Tooltip("유닛 이름 텍스트")]
    [SerializeField] private TMP_Text unitNameText;

    [Tooltip("현재 계산된 전투력 텍스트 (예: 90 또는 전투력: 90)")]
    [SerializeField] private TMP_Text combatPowerText;

    [Header("--- 시각 효과 설정 ---")]
    [Tooltip("현재 던전 또는 다른 던전 파견 중일 때의 초상화/배경 틴트 색상")]
    [SerializeField] private Color assignedDimColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

    [Tooltip("미배치 상태일 때의 기본 색상")]
    [SerializeField] private Color normalColor = Color.white;

    #endregion

    #region 내부 상태 필드

    private int _unitId;
    private Action<int> _onClickCallback;

    #endregion

    #region 라이프사이클 및 이벤트 바인딩

    // 버튼 클릭 리스너 등록
    private void Awake()
    {
        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
        }

        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnClickCard);
        }
    }

    // 버튼 클릭 리스너 해제
    private void OnDestroy()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnClickCard);
        }
    }

    // 카드 클릭 이벤트 핸들러
    private void OnClickCard()
    {
        _onClickCallback?.Invoke(_unitId);
    }

    #endregion

    #region 데이터 바인딩

    // 유닛 데이터 바인딩 및 파견 딤드 처리
    public void Bind(
        int unitId,
        string unitName,
        Sprite icon,
        int combatPower,
        bool isAssignedInAnyDungeon,
        Action<int> onClick)
    {
        _unitId = unitId;
        _onClickCallback = onClick;

        if (unitNameText != null)
        {
            unitNameText.text = unitName;
        }

        if (combatPowerText != null)
        {
            combatPowerText.text = $"전투력 {combatPower}";
        }

        if (portraitImage != null)
        {
            if (icon != null)
            {
                portraitImage.sprite = icon;
                portraitImage.enabled = true;
            }
            else
            {
                portraitImage.enabled = false;
            }

            portraitImage.color = isAssignedInAnyDungeon ? assignedDimColor : normalColor;
        }
    }

    #endregion
}
