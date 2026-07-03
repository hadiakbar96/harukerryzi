using UnityEngine;

public class PackSequenceController : MonoBehaviour
{
    [Header("Animators")]
    public Animator cameraAnimator;
    public Animator packAnimator;

    public void StartSequence()
    {
        cameraAnimator.SetTrigger("Zoom");
    }

    // Called by Camera Animation Event
    public void PlayPackOpen()
    {
        packAnimator.SetTrigger("Open");
    }
}