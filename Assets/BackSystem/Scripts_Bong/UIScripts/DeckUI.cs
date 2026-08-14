using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.UI;

// 메인 UI 하단에 배치된 10개 덱 슬롯의 유닛 초상화 이미지 및 활성화 상태를 관리하는 UI 컨트롤러
public class DeckUI : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 캐릭터 초상화 카탈로그 ---")]
    [Tooltip("유닛 ID별 초상화 스프라이트 매핑 정보를 담고 있는 SO")]
    [SerializeField] private UnitPortraitCatalogSO portraitCatalog;

    [Header("--- 덱 이미지 표시 오브젝트 (총 10개 슬롯) ---")]
    [Tooltip("하단 UI의 덱 슬롯 게임 오브젝트 목록 (DeckSlot_01 ~ DeckSlot_10)")]
    [SerializeField] private GameObject[] deckPrefabs = new GameObject[10];

    #endregion

    #region 내부 캐시 필드

    // 슬롯별 캐릭터 카드 아트워크 이미지 컴포넌트 캐시 배열 (총 10개)
    private Image[] _deckArtworkImages = new Image[10];
    // 현재 적용된 덱 슬롯 데이터 캐시
    private int[] _currentDeckSlots = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    #endregion

    #region 라이프사이클

    // 컴포넌트 초기화 및 카탈로그 / 이미지 컴포넌트 캐싱
    private void Awake()
    {
        // 1. 초상화 카탈로그 미할당 시 Resources 폴더에서 자동 로드
        if (portraitCatalog == null)
        {
            portraitCatalog = Resources.Load<UnitPortraitCatalogSO>("UnitPortraitCatalog");
        }

        // 2. 인스펙터에 슬롯 오브젝트가 바인딩되지 않은 경우 자식 계층 구조에서 자동 탐색
        InitializeSlotObjectsAndImages();
    }

    // 전역 세이브/로드 및 덱 변경 이벤트 버스 구독
    private void OnEnable()
    {
        // 덱 변경 이벤트 구독
        EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoad);
        EventBus.Subscribe<DataSaveEvent>(OnDataSave);

        // UI 활성화 시점에 DeckManager의 최신 덱 슬롯 정보로 갱신
        RefreshUIWithCurrentData();
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnDataSave);
    }

    private void Start()
    {
        RefreshUIWithCurrentData();
    }

    #endregion

    #region 초기화 보조 메서드

    // 슬롯 게임오브젝트와 내부 Image 컴포넌트들을 캐싱
    private void InitializeSlotObjectsAndImages()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject slotObj = (deckPrefabs != null && i < deckPrefabs.Length) ? deckPrefabs[i] : null;

            // 인스펙터 바인딩이 비어있다면 자식 계층에서 DeckSlot_xx 이름으로 탐색 시도
            if (slotObj == null)
            {
                string slotName = $"DeckSlot_{i + 1:D2}";
                Transform childTransform = transform.Find(slotName);
                if (childTransform != null)
                {
                    slotObj = childTransform.gameObject;
                    if (deckPrefabs != null && i < deckPrefabs.Length)
                    {
                        deckPrefabs[i] = slotObj;
                    }
                }
            }

            if (slotObj != null)
            {
                // 슬롯 내부의 'CardArtworkImage' 컴포넌트 우선 탐색 (없으면 슬롯의 Image 컴포넌트 사용)
                Image artworkImg = null;
                Transform artworkTrans = slotObj.transform.Find("CardArtworkImage");
                if (artworkTrans != null)
                {
                    artworkImg = artworkTrans.GetComponent<Image>();
                }

                if (artworkImg == null)
                {
                    // 자식 중 Image 컴포넌트 탐색
                    artworkImg = slotObj.GetComponentInChildren<Image>(true);
                }

                _deckArtworkImages[i] = artworkImg;
            }
        }
    }

    #endregion

    #region 이벤트 수신 및 UI 갱신

    // 덱 변경 이벤트 수신 시 UI 즉시 갱신
    private void OnDeckChanged(DeckChangedEvent evt)
    {
        if (evt.deckSlots != null)
        {
            UpdateDeckUI(evt.deckSlots);
        }
    }

    // 세이브 데이터 로드 이벤트 수신
    private void OnDataLoad(DataLoadEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null && evt.saveData.unitDeck.deckSlots != null)
        {
            UpdateDeckUI(evt.saveData.unitDeck.deckSlots);
        }
    }

    // 세이브 데이터 저장 이벤트 수신
    private void OnDataSave(DataSaveEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null && evt.saveData.unitDeck.deckSlots != null)
        {
            UpdateDeckUI(evt.saveData.unitDeck.deckSlots);
        }
    }

    // DeckManager의 최신 덱 데이터로 UI 갱신
    private void RefreshUIWithCurrentData()
    {
        if (DeckManager.Instance != null)
        {
            UpdateDeckUI(DeckManager.Instance.GetDeckSlotsCopy());
        }
    }

    // 외부 매니저에서 덱 슬롯 배열을 전달받아 UI를 갱신하는 공용 메서드
    public void UpdateDeckUI(int[] deckSlots)
    {
        if (deckSlots == null)
        {
            return;
        }

        int length = Math.Min(10, deckSlots.Length);
        for (int i = 0; i < length; i++)
        {
            _currentDeckSlots[i] = deckSlots[i];
        }

        ApplyDeckSlotsToUI(_currentDeckSlots);
    }

    // 10개 덱 슬롯에 대한 이미지 바인딩 및 활성화/비활성화 처리
    private void ApplyDeckSlotsToUI(int[] deckSlots)
    {
        if (deckSlots == null)
        {
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            GameObject slotObj = (deckPrefabs != null && i < deckPrefabs.Length) ? deckPrefabs[i] : null;
            if (slotObj == null)
            {
                continue;
            }

            int unitRawId = (i < deckSlots.Length) ? deckSlots[i] : -1;

            // 1. 덱에 유닛이 등록되지 않은 슬롯 (rawId <= 0 또는 -1) -> 오브젝트 비활성화
            if (unitRawId <= 0)
            {
                slotObj.SetActive(false);
                continue;
            }

            // 2. 덱에 유닛이 등록된 슬롯 (rawId > 0) -> 오브젝트 활성화 및 이미지 바인딩
            slotObj.SetActive(true);

            Image artworkImage = _deckArtworkImages[i];
            if (artworkImage != null)
            {
                string unitIdStr = $"UNIT_{unitRawId:D4}";
                Sprite portraitSprite = null;

                // 초상화 카탈로그에서 해당 유닛의 초상화 스프라이트 조회
                if (portraitCatalog != null)
                {
                    portraitSprite = portraitCatalog.GetPortraitByUnitId(unitIdStr);
                }

                if (portraitSprite != null)
                {
                    artworkImage.sprite = portraitSprite;
                    artworkImage.enabled = true;
                    // 스프라이트 고유 색상 유지를 위해 기본 흰색 알파 1로 설정
                    artworkImage.color = Color.white;
                }
                else
                {
                    // 초상화 리소스가 없는 경우 기본 색상 유지
                    artworkImage.sprite = null;
                }
            }
        }
    }

    #endregion
}
