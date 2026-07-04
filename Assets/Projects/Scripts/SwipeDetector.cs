using UnityEngine;

public class SwipeDetector : MonoBehaviour
{
    private Vector2 startPosition;
    private Vector2 endPosition;

    private bool startedOnSliceZone = false;

    private PackController packController;
    private SwipeTooltip swipeTooltip;

    private void Start()
    {
        packController = GetComponent<PackController>();
        swipeTooltip = GetComponent<SwipeTooltip>();
    }

    private void Update()
    {
        // Mouse Button Down
        if (Input.GetMouseButtonDown(0))
        {
            startPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Collider2D hit = Physics2D.OverlapPoint(startPosition);

            Debug.Log(hit);
            if (hit != null && hit.CompareTag("SliceZone"))
            {
                startedOnSliceZone = true;
                Debug.Log("Started on Slice Zone");
            }
            else
            {
                startedOnSliceZone = false;
            }
        }

        // Mouse Button Up
        if (Input.GetMouseButtonUp(0))
        {
            if (!startedOnSliceZone)
                return;

            endPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector2 swipe = endPosition - startPosition;

            if (swipe.x > 2f && Mathf.Abs(swipe.y) < 1f)
            {
                // Hide the tooltip before opening the pack
                if (swipeTooltip != null)
                    swipeTooltip.Hide();

                packController.OpenPack();
            }

            // Reset for the next swipe
            startedOnSliceZone = false;
        }
    }
}