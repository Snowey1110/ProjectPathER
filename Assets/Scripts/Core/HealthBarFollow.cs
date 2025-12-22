using UnityEngine;

public class HealthBarFollow : MonoBehaviour
{
    public Transform objectToFollow;
    private RectTransform rectTransform;
    private Canvas canvas;

    [Tooltip("If null, will use Camera.main (the owning player's camera on each client).")]
    public Camera worldCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void LateUpdate()
    {
        if (objectToFollow == null)
        {
            Destroy(transform.parent.gameObject);
            return;
        }

        if (canvas == null) return;

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(objectToFollow.position);

        // Hide if behind camera
        if (screenPos.z < 0f)
        {
            rectTransform.gameObject.SetActive(false);
            return;
        }

        rectTransform.gameObject.SetActive(true);

        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out Vector2 localPoint);

        rectTransform.anchoredPosition = localPoint;
    }
}
