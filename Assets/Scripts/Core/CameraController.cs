using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform player; // Automatically assigned by PlayerController

    [Header("Settings")]
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;
    public Vector2 panLimit; // Limit how far you can pan (x, y)

    [Header("Zoom")]
    public float zoomSpeed = 5f; // Lower number = smoother
    public float minZoom = 5f;
    public float maxZoom = 20f;
    private float targetZoom;

    [Header("Smoothing")]
    public float smoothTime = 0.2f; // Time to reach target
    private Vector3 velocity = Vector3.zero;

    // State
    private bool isLocked = true; // Default to locked on player
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize;

        // CRITICAL: If this camera is a child of the Player prefab, 
        // we must detach it so it can move independently!
        transform.parent = null;
    }

    void LateUpdate() // Camera should always be in LateUpdate to track Physics smoothly
    {
        if (player == null) return;

        HandleInput();
        HandleMovement();
        HandleZoom();
    }

    void HandleInput()
    {
        // Toggle Lock Mode with 'Y' (Standard MOBA/RPG bind)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            isLocked = !isLocked;
        }

        // Hold Space to temporarily center
        if (Input.GetKey(KeyCode.Space))
        {
            isLocked = true;
        }
    }

    void HandleMovement()
    {
        Vector3 targetPos = transform.position;

        if (isLocked)
        {
            // LOCKED MODE: Follow the player
            targetPos = player.position;
            targetPos.z = -10; // Standard 2D camera depth
        }
        else
        {
            // FREE MODE: Edge Panning
            Vector3 mousePos = Input.mousePosition;

            if (mousePos.y >= Screen.height - panBorderThickness)
                targetPos.y += panSpeed * Time.deltaTime;

            if (mousePos.y <= panBorderThickness)
                targetPos.y -= panSpeed * Time.deltaTime;

            if (mousePos.x >= Screen.width - panBorderThickness)
                targetPos.x += panSpeed * Time.deltaTime;

            if (mousePos.x <= panBorderThickness)
                targetPos.x -= panSpeed * Time.deltaTime;
        }

        // Apply Smoothing (This prevents jitter!)
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Calculate target zoom (Don't apply instantly)
        if (scroll != 0)
        {
            targetZoom -= scroll * 1000 * Time.deltaTime; // Sensitivity
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // Smoothly interpolate current zoom to target zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }
}