using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gacha history scrollbar arrow buttons. Kept separate from the gacha logic so
/// the integration scene can change its presentation without changing draws.
/// </summary>
public sealed class GachaScrollbarArrowStepper : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField, Range(0.01f, 1f)] private float step = 0.2f;

    public void Configure(ScrollRect target, float normalizedStep = 0.2f)
    {
        scrollRect = target;
        step = Mathf.Clamp01(normalizedStep);
    }

    public void ScrollUp()
    {
        Move(step);
    }

    public void ScrollDown()
    {
        Move(-step);
    }

    private void Move(float delta)
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta);
    }
}
