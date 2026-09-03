using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public enum TutorialStep
    {
        Intro,

        Gacha,
        Deck,
        UnitStorage,
        Upgrade,
        Inventory,
        Craft,
        Dungeon,
        AutoBattle,
        Raid,

        Complete
    }

    [SerializeField] private TutorialStep currentStep = TutorialStep.Intro;

    public TutorialStep CurrentStep => currentStep;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetStep(TutorialStep step)
    {
        currentStep = step;

        Debug.Log($"[Tutorial] Step 변경: {currentStep}");
    }
    
    public void NextStep()
    {
        currentStep++;

        Debug.Log($"[Tutorial] 현재 단계: {currentStep}");
    }
}