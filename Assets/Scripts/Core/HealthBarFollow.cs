using UnityEngine;

public class HealthBarFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform objectToFollow;

    [Header("World Offset")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Billboard")]
    [SerializeField] private bool faceCamera = false;
    [SerializeField] private Camera worldCamera;

    [Header("Cleanup")]
    [SerializeField] private float destroyGraceSeconds = 0.5f;
    private float aliveTimer;

    private void OnEnable()
    {
        aliveTimer = 0f;

        // Auto-assign if not set
        if (objectToFollow == null)
            objectToFollow = transform.root;
    }

    private void LateUpdate()
    {
        aliveTimer += Time.deltaTime;

        if (objectToFollow == null)
        {
            if (aliveTimer >= destroyGraceSeconds)
                Destroy(gameObject); // only destroy the bar itself
            return;
        }

        // World-space follow
        transform.position = objectToFollow.position + worldOffset;

        // Optional: keep UI facing camera (usually not needed for 2D orthographic)
        if (faceCamera)
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera != null)
                transform.forward = worldCamera.transform.forward;
        }
    }
}
