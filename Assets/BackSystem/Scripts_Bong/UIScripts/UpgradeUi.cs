using System;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUi : MonoBehaviour
{
    #region 노출 변수

    [Header("업그레이드 수치")]
    [SerializeField] private int upgradeIndex = 1;
    [Space(5f),Header("참조")]
    [SerializeField] private Button[] upgradeButtons;

    #endregion

    #region 이벤트

    public static event Action<int,int> OnCurrencyUpgrade;

    #endregion
    
    #region 라이프 사이클

    private void Awake()
    {
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            int index = i;
            upgradeButtons[i].onClick.AddListener((() => OnCurrencyUpgrade?.Invoke(index,upgradeIndex)));
        }
    }

    #endregion
    
    
}
