using UnityEngine;

public class CameraAnimationEvents : MonoBehaviour
{
    private PackController packController;
    private PackSequenceController sequenceController;
    public Animator bottomPackAnimator;

    private void Awake()
    {
        packController = FindFirstObjectByType<PackController>();
        sequenceController = FindFirstObjectByType<PackSequenceController>();
    }

    public void RevealCard()
    {
        packController.RevealCard();
    }

    public void PlayBottomPack()
    {
        bottomPackAnimator.SetTrigger("MoveDown");
    }

    public void PlayPackOpen()
    {
        sequenceController.PlayPackOpen();
    }
}