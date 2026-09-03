using TMPro;
using UnityEngine;

public class MainLobbyTutorialUI : MonoBehaviour
{
    
    [SerializeField] private RectTransform highlight;
    
    [Header("Tutorial Targets")]
    [SerializeField] private RectTransform gachaButton;
    [SerializeField] private RectTransform deckButton;
    [SerializeField] private RectTransform inventoryButton;
    [SerializeField] private RectTransform dungeonButton;
    [SerializeField] private RectTransform autoBattleButton;
    [SerializeField] private RectTransform raidButton;
    [SerializeField] private RectTransform unitStorageButton;
    [SerializeField] private RectTransform upgradeButton;
    [SerializeField] private RectTransform craftButton;
    
    [SerializeField] private GameObject tutorialOverlay;
    
    [SerializeField] private TMP_Text tutorialText;

   
    
    public void BeginTutorial()
    {
        if (TutorialManager.Instance == null) return;

        TutorialManager.Instance.SetStep(TutorialManager.TutorialStep.Intro);

        if (tutorialOverlay != null)  tutorialOverlay.SetActive(true);
        

        UpdateTutorialText();
    }

    public void OnClickNext()
    {
        if (TutorialManager.Instance == null) return;

        if (TutorialManager.Instance.CurrentStep
            == TutorialManager.TutorialStep.Complete)
        {
            if (tutorialOverlay != null)
            {
                tutorialOverlay.SetActive(false);
            }

            return;
        }

        TutorialManager.Instance.NextStep();
        UpdateTutorialText();
    }

    private void UpdateTutorialText()
    {
        if (TutorialManager.Instance == null) return;
        if (tutorialText == null) return;

        switch (TutorialManager.Instance.CurrentStep)
        {
            case TutorialManager.TutorialStep.Intro:
                tutorialText.text =
                    "안녕하세요! 엔드리스 가드에 오신 것을 환영합니다.";
                break;
            
            

                       case TutorialManager.TutorialStep.Gacha:
                tutorialText.text =
                    "캐릭터 뽑기에서는 새로운 캐릭터를 획득할 수 있습니다.";
                break;

            case TutorialManager.TutorialStep.Deck:
                tutorialText.text =
                    "덱 편성에서는 전투에 사용할 캐릭터를 선택하고, 편성 구성을 변경할 수 있습니다.";
                break;

            case TutorialManager.TutorialStep.Inventory:
                tutorialText.text =
                    "인벤토리에서는 획득한 장비와 아이템을 확인하고 관리할 수 있습니다.";
                break;
            
            case TutorialManager.TutorialStep.UnitStorage:
                tutorialText.text =
                    "유닛 보관함에서는 보유하고 있는 캐릭터를 확인할 수 있습니다.";
                break;

            case TutorialManager.TutorialStep.Upgrade:
                tutorialText.text =
                    "업그레이드에서는 캐릭터를 성장시켜 전투 능력을 강화할 수 있습니다.";
                break;

            case TutorialManager.TutorialStep.Craft:
                tutorialText.text =
                    "공방에서는 전투와 성장에 필요한 아이템을 제작할 수 있습니다.";
                break;

            case TutorialManager.TutorialStep.Dungeon:
                tutorialText.text =
                    "던전에서는 다양한 전투 콘텐츠에 도전하고 보상을 획득할 수 있습니다.";
                break;

            case TutorialManager.TutorialStep.AutoBattle:
                tutorialText.text =
                    "자동 전투에서는 편성한 캐릭터들이 자동으로 전투를 진행하며 보상을 획득합니다.";
                break;

            case TutorialManager.TutorialStep.Raid:
                tutorialText.text =
                    "레이드에서는 강력한 보스를 상대로 전투를 진행하고 특별한 보상을 획득할 수 있습니다.";
                break;

            case TutorialManager.TutorialStep.Complete:
                tutorialText.text =
                    "기본적인 안내가 끝났습니다. 이제 자유롭게 게임을 즐겨보세요!";
                break;
        }
        UpdateHighlight();
    }
    
    private void UpdateHighlight()
    {
        if (highlight == null) return;
        if (TutorialManager.Instance == null) return;

        RectTransform target = null;

        switch (TutorialManager.Instance.CurrentStep)
        {
            case TutorialManager.TutorialStep.Gacha:
                target = gachaButton;
                break;

            case TutorialManager.TutorialStep.Deck:
                target = deckButton;
                break;
            
            case TutorialManager.TutorialStep.UnitStorage:
                target = unitStorageButton;
                break;

            case TutorialManager.TutorialStep.Upgrade:
                target = upgradeButton;
                break;

            case TutorialManager.TutorialStep.Craft:
                target = craftButton;
                break;

            case TutorialManager.TutorialStep.Inventory:
                target = inventoryButton;
                break;

            case TutorialManager.TutorialStep.Dungeon:
                target = dungeonButton;
                break;

            case TutorialManager.TutorialStep.AutoBattle:
                target = autoBattleButton;
                break;

            case TutorialManager.TutorialStep.Raid:
                target = raidButton;
                break;
        }

        if (target == null)
        {
            highlight.gameObject.SetActive(false);
            return;
        }

        highlight.gameObject.SetActive(true);

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        RectTransform highlightParent = highlight.parent as RectTransform;

        if (highlightParent == null) return;

        Vector3 bottomLeft = highlightParent.InverseTransformPoint(corners[0]);
        Vector3 topRight = highlightParent.InverseTransformPoint(corners[2]);
        Vector3 center = (bottomLeft + topRight) * 0.5f;
        Vector2 size = new Vector2(topRight.x - bottomLeft.x, topRight.y - bottomLeft.y );

        highlight.anchoredPosition = center;
        highlight.sizeDelta =  size + new Vector2(50f, 30f);
    }
}