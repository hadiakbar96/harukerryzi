using UnityEngine;

namespace Harukerryzi.Clash
{
    [ExecuteAlways]
    public sealed class CanvasBoundsGizmo : MonoBehaviour
    {
        [SerializeField] private Color color = new(1f, 0.85f, 0.1f, 1f);

        private readonly Vector3[] corners = new Vector3[4];

        private void OnDrawGizmos()
        {
            if (!TryGetComponent(out RectTransform rectTransform))
            {
                return;
            }

            rectTransform.GetWorldCorners(corners);
            Gizmos.color = color;

            for (int i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
            }
        }
    }
}
