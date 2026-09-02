using System;
using System.Collections.Generic;
using EndlessGuard.Unit.Data;
using UnityEngine;
using UnityEngine.UI;

// 인게임 UI의 덱 슬롯에 유닛 초상화 및 활성화/소환 상태를 실시간 표시하는 UI 컨트롤러
public class DeckUI : MonoBehaviour
{
    #region 직렬화 변수 (인스펙터 바인딩)

    [Header("--- 덱 타겟 설정 ---")]
    [Tooltip("표시할 덱 종류 (Normal: 일반 필드 덱, Raid1/Raid2: 레이드 덱)")]
    [SerializeField] private DeckType targetDeckType = DeckType.Normal;

    [Header("--- 캐릭터 초상화 카탈로그 ---")]
    [Tooltip("유닛 ID별 초상화 스프라이트 매핑 정보를 담고 있는 SO")]
    [SerializeField] private UnitPortraitCatalogSO portraitCatalog;

    [Header("--- 덱 이미지 표시 오브젝트 (기본 10개 슬롯) ---")]
    [Tooltip("UI 슬롯 게임 오브젝트 목록 (DeckSlot_01 ~ DeckSlot_10)")]
    [SerializeField] private GameObject[] deckPrefabs = new GameObject[10];

    #endregion

    #region 내부 캐시 필드

    private Image[] _deckArtworkImages = new Image[10];
    private int[] _currentDeckSlots = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    private readonly HashSet<string> _spawnedUnitKeys = new HashSet<string>();

    #endregion

    #region 프로퍼티

    public DeckType TargetDeckType => targetDeckType;

    #endregion

    #region 라이프사이클

    // 컴포넌트 초기화 및 카탈로그 / 이미지 컴포넌트 캐싱
    private void Awake()
    {
        if (portraitCatalog == null)
        {
            portraitCatalog = CollectionDataProvider.Instance != null 
                ? CollectionDataProvider.Instance.PortraitCatalog 
                : Resources.Load<UnitPortraitCatalogSO>("UnitPortraitCatalog");
        }

        InitializeSlotObjectsAndImages();
    }

    // 전역 세이브/로드 및 덱 변경 이벤트 버스 구독
    private void OnEnable()
    {
        EventBus.Subscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        EventBus.Subscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Subscribe<DeckChangedEvent>(OnDeckChanged);
        EventBus.Subscribe<DataLoadEvent>(OnDataLoad);
        EventBus.Subscribe<DataSaveEvent>(OnDataSave);
        EventBus.Subscribe<UnitFieldSpawnStateChangedEvent>(OnUnitFieldSpawnStateChanged);

        RefreshUIWithCurrentData();
    }

    // 이벤트 버스 구독 해제
    private void OnDisable()
    {
        EventBus.Unsubscribe<NormalDeckChangedEvent>(OnNormalDeckChanged);
        EventBus.Unsubscribe<RaidDeckChangedEvent>(OnRaidDeckChanged);
        EventBus.Unsubscribe<DeckChangedEvent>(OnDeckChanged);
        EventBus.Unsubscribe<DataLoadEvent>(OnDataLoad);
        EventBus.Unsubscribe<DataSaveEvent>(OnDataSave);
        EventBus.Unsubscribe<UnitFieldSpawnStateChangedEvent>(OnUnitFieldSpawnStateChanged);
    }

    private void Start()
    {
        RefreshUIWithCurrentData();
    }

    #endregion

    #region 초기화 보조 메서드

    // 슬롯 게임오브젝트와 내부 Image 컴포넌트 캐싱
    private void InitializeSlotObjectsAndImages()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject slotObj = (deckPrefabs != null && i < deckPrefabs.Length) ? deckPrefabs[i] : null;

            if (slotObj == null)
            {
                string slotName = string.Format("DeckSlot_{0:D2}", i + 1);
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
                Image artworkImg = null;
                Transform artworkTrans = slotObj.transform.Find("CardArtworkImage");
                if (artworkTrans != null)
                {
                    artworkImg = artworkTrans.GetComponent<Image>();
                }

                if (artworkImg == null)
                {
                    artworkImg = slotObj.GetComponentInChildren<Image>(true);
                }

                _deckArtworkImages[i] = artworkImg;
            }
        }
    }

    #endregion

    #region 이벤트 수신 및 UI 갱신

    // 일반 덱 변경 이벤트 수신 처리
    private void OnNormalDeckChanged(NormalDeckChangedEvent evt)
    {
        if (targetDeckType == DeckType.Normal)
        {
            ApplySlotEntriesToUI(evt.allSlots);
        }
    }

    // 레이드 덱 변경 이벤트 수신 처리
    private void OnRaidDeckChanged(RaidDeckChangedEvent evt)
    {
        if (evt.raidTeamType == targetDeckType)
        {
            ApplySlotEntriesToUI(evt.allSlots);
        }
    }

    // 레거시 덱 변경 이벤트 수신 처리
    private void OnDeckChanged(DeckChangedEvent evt)
    {
        if (evt.deckType == targetDeckType && evt.deckSlots != null)
        {
            UpdateDeckUI(evt.deckSlots);
        }
    }

    // 세이브 데이터 로드 이벤트 수신 처리
    private void OnDataLoad(DataLoadEvent evt)
    {
        if (evt.saveData != null && evt.saveData.unitDeck != null)
        {
            int[] targetSlots = targetDeckType switch
            {
                DeckType.Normal => evt.saveData.unitDeck.normalDeckSlots,
                DeckType.Raid1 => evt.saveData.unitDeck.raid1DeckSlots,
                DeckType.Raid2 => evt.saveData.unitDeck.raid2DeckSlots,
                _ => evt.saveData.unitDeck.normalDeckSlots
            };

            if (targetSlots != null)
            {
                UpdateDeckUI(targetSlots);
            }
        }
    }

    // 세이브 데이터 저장 이벤트 수신 처리
    private void OnDataSave(DataSaveEvent evt)
    {
        RefreshUIWithCurrentData();
    }

    // 타겟 덱 종류 동적 변경 및 UI 동기화
    public void SetTargetDeckType(DeckType newDeckType)
    {
        targetDeckType = newDeckType;
        RefreshUIWithCurrentData();
    }

    // 현재 덱 데이터 기반 UI 갱신 연산
    public void RefreshUIWithCurrentData()
    {
        if (DeckManager.Instance != null)
        {
            UpdateDeckUI(DeckManager.Instance.GetDeckSlotsCopy(targetDeckType));
        }
    }

    // 슬롯 상세 엔트리 목록 기반 UI 갱신 연산
    public void ApplySlotEntriesToUI(IReadOnlyList<DeckSlotUnitEntry> slotEntries)
    {
        if (slotEntries == null) return;

        for (int i = 0; i < 10; i++)
        {
            GameObject slotObj = (deckPrefabs != null && i < deckPrefabs.Length) ? deckPrefabs[i] : null;
            if (slotObj == null) continue;

            DeckSlotUnitEntry entry = (i < slotEntries.Count) ? slotEntries[i] : default;

            if (!entry.isOccupied || entry.unitId <= 0)
            {
                slotObj.SetActive(false);
                continue;
            }

            slotObj.SetActive(true);

            Image artworkImage = _deckArtworkImages[i];
            if (artworkImage != null)
            {
                Sprite portraitSprite = null;
                if (portraitCatalog != null)
                {
                    portraitSprite = portraitCatalog.GetPortraitByUnitId(entry.unitKey);
                }

                if (portraitSprite != null)
                {
                    artworkImage.sprite = portraitSprite;
                    artworkImage.enabled = true;
                    bool isSpawned = _spawnedUnitKeys.Contains(entry.unitKey);
                    artworkImage.color = isSpawned ? new Color(0.35f, 0.35f, 0.35f, 1.0f) : Color.white;
                }
                else
                {
                    artworkImage.sprite = null;
                }
            }
        }
    }

    // 아군 유닛 필드 소환/사망 상태 변경 이벤트 수신 시 색상 갱신
    private void OnUnitFieldSpawnStateChanged(UnitFieldSpawnStateChangedEvent evt)
    {
        if (evt.isSpawned)
        {
            _spawnedUnitKeys.Add(evt.unitKey);
        }
        else
        {
            _spawnedUnitKeys.Remove(evt.unitKey);
        }

        RefreshSlotColors();
    }

    // 슬롯 초상화 명암 색상 동기화 연산
    private void RefreshSlotColors()
    {
        for (int i = 0; i < 10; i++)
        {
            Image artworkImage = _deckArtworkImages[i];
            if (artworkImage == null || !artworkImage.enabled || artworkImage.sprite == null)
            {
                continue;
            }

            int unitRawId = (i < _currentDeckSlots.Length) ? _currentDeckSlots[i] : -1;
            if (unitRawId <= 0) continue;

            string unitKey = UnitIdHelper.ToUnitKey(unitRawId);
            bool isSpawned = _spawnedUnitKeys.Contains(unitKey);
            artworkImage.color = isSpawned ? new Color(0.35f, 0.35f, 0.35f, 1.0f) : Color.white;
        }
    }

    // 정수 덱 슬롯 배열 기반 UI 갱신 연산
    public void UpdateDeckUI(int[] deckSlots)
    {
        if (deckSlots == null) return;

        // 8칸 덱(레이드)으로 전환 시 이전 10칸 덱(일반)의 잔존 유닛이 8, 9번 슬롯에 켜지는 현상을 방지하기 위해 전체 -1로 초기화
        for (int i = 0; i < 10; i++)
        {
            _currentDeckSlots[i] = -1;
        }

        int length = Math.Min(10, deckSlots.Length);
        for (int i = 0; i < length; i++)
        {
            _currentDeckSlots[i] = deckSlots[i];
        }

        ApplyDeckSlotsToUI(_currentDeckSlots);
    }

    // 10개 덱 슬롯 이미지 바인딩 및 활성화 처리
    private void ApplyDeckSlotsToUI(int[] deckSlots)
    {
        if (deckSlots == null) return;

        for (int i = 0; i < 10; i++)
        {
            GameObject slotObj = (deckPrefabs != null && i < deckPrefabs.Length) ? deckPrefabs[i] : null;
            if (slotObj == null) continue;

            int unitRawId = (i < deckSlots.Length) ? deckSlots[i] : -1;

            if (unitRawId <= 0)
            {
                slotObj.SetActive(false);
                continue;
            }

            slotObj.SetActive(true);

            Image artworkImage = _deckArtworkImages[i];
            if (artworkImage != null)
            {
                string unitIdStr = UnitIdHelper.ToUnitKey(unitRawId);
                Sprite portraitSprite = null;

                if (portraitCatalog != null)
                {
                    portraitSprite = portraitCatalog.GetPortraitByUnitId(unitIdStr);
                }

                if (portraitSprite != null)
                {
                    artworkImage.sprite = portraitSprite;
                    artworkImage.enabled = true;
                    bool isSpawned = _spawnedUnitKeys.Contains(unitIdStr);
                    artworkImage.color = isSpawned ? new Color(0.35f, 0.35f, 0.35f, 1.0f) : Color.white;
                }
                else
                {
                    artworkImage.sprite = null;
                }
            }
        }
    }

    #endregion
}
