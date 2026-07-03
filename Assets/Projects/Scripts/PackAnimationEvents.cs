using UnityEngine;

public class PackAnimationEvents : MonoBehaviour
{
    private PackController packController;
    public Animator cameraAnimator;

    private void Awake()
    {
        packController = GetComponentInParent<PackController>();
    }

    public void StartCameraZoomOut()
    {
        cameraAnimator.SetTrigger("ZoomOut");
    }
}