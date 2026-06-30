using UnityEngine;

public class BottomPackAnimationEvents : MonoBehaviour
{
    private PackController packController;

    private void Awake()
    {
        packController = GetComponentInParent<PackController>();
    }

    public void RevealCard()
    {
        packController.RevealCard();
    }
}