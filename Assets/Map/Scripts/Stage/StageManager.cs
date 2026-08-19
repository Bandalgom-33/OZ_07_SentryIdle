using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Stage 설정")] [SerializeField] private int currtenStage = 1;

    //스테이지는 5스테이지로 고정
    private const int wavesPerStage = 5;
    
    public int CurrentStage =>  currtenStage;
    public int WavesPerStage => wavesPerStage;

    public void ClearStage()
    {
        //스테이지를 클리어하면 현재 스테이지 +1
        currtenStage++;
    }
}
