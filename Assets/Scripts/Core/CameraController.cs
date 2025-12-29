using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform player; // Automatically assigned by PlayerController

    [Header("Settings")]
    public float panSpeed = 200f;
    public float panBorderThickness = 10f;
    public Vector2 panLimit; // Limit how far you can pan (x, y)

    [Header("Unlocked Speed")]
    [Tooltip("Multiplier applied to panSpeed when unlocked (Y toggled).")]
    public float unlockedPanSpeedMult = 3.0f;

    [Tooltip("Additional multiplier when mouse is inside the border zone (unlocked only).")]
    public float edgePanSpeedMult = 1.75f;

    [Header("Soft Follow Back (Optional)")]
    [Tooltip("If true, when re-locking (Y -> lock or Space), camera eases back to player once, then hard-locks.")]
    public bool softFollowBackOnRelock = false;

    [Tooltip("Smooth time used only while easing back to player on relock.")]
    public float softFollowBackSmoothTime = 0.08f;

    [Tooltip("When camera gets within this distance of the player, it snaps and resumes hard lock.")]
    public float softFollowBackSnapDistance = 0.03f;

    [Header("Zoom")]
    public float zoomSpeed = 5f; // Lower number = smoother
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Tooltip("Scale Input System scroll (Mouse.scroll.y) into a value similar to old Mouse ScrollWheel axis.")]
    public float scrollScale = 0.1f;

    private float targetZoom;

    [Header("Smoothing")]
    public float smoothTime = 0.2f; // Time to reach target (unlocked panning)
    private Vector3 velocity = Vector3.zero;

    [Header("Locked Follow Mode")]
    [Tooltip("If true, when locked the camera follows the player using SmoothDamp (like before). If false, it hard-locks exactly to player.")]
    public bool lockedFollowSmooth = true;

    [Tooltip("SmoothDamp time used while lockedFollowSmooth is enabled.")]
    public float lockedFollowSmoothTime = 0.08f;

    [Tooltip("If camera is farther than this from player while lockedFollowSmooth, snap to player to avoid big lag.")]
    public float lockedFollowSnapDistance = 2.0f;

    // State
    private bool isLocked = true; // Default to locked on player
    private bool _softReturningToPlayer = true; // relock easing state
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize;

        // CRITICAL: If this camera is a child of the Player prefab,
        // we must detach it so it can move independently!
        transform.parent = null;
    }

    void LateUpdate()
    {
        if (player == null) return;

        HandleInput();
        HandleMovement();
        HandleZoom();
    }

    void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Toggle Lock Mode with 'Y'
        if (kb.yKey.wasPressedThisFrame)
        {
            bool wasLocked = isLocked;
            isLocked = !isLocked;

            if (!wasLocked && isLocked)
            {
                // just transitioned from unlocked -> locked
                velocity = Vector3.zero;
                _softReturningToPlayer = softFollowBackOnRelock;
            }
            else if (wasLocked && !isLocked)
            {
                // locked -> unlocked
                _softReturningToPlayer = false;
            }
        }

        // Hold Space to temporarily center
        if (kb.spaceKey.isPressed)
        {
            if (!isLocked)
            {
                isLocked = true;
                velocity = Vector3.zero;
                _softReturningToPlayer = softFollowBackOnRelock;
            }
        }
    }

    void HandleMovement()
    {
        if (isLocked)
        {
            Vector3 p = player.position;
            p.z = -10f;

            // Smooth follow while locked (old behavior)
            if (lockedFollowSmooth)
            {
                // Prevent huge trailing if we got far away (e.g., relock after panning)
                Vector2 d = (Vector2)(transform.position - p);
                if (d.sqrMagnitude > lockedFollowSnapDistance * lockedFollowSnapDistance)
                {
                    transform.position = p;
                    velocity = Vector3.zero;
                    _softReturningToPlayer = false;
                    return;
                }

                // If we are doing a one-time "soft follow back", use that smooth time until it finishes
                float st = _softReturningToPlayer ? softFollowBackSmoothTime : lockedFollowSmoothTime;

                Vector3 next = Vector3.SmoothDamp(transform.position, p, ref velocity, st);
                next.z = -10f;
                transform.position = next;

                // If we were in soft-return mode, finish it when close enough, then continue locked smooth follow normally
                if (_softReturningToPlayer)
                {
                    Vector2 dd = (Vector2)(transform.position - p);
                    if (dd.sqrMagnitude <= softFollowBackSnapDistance * softFollowBackSnapDistance)
                    {
                        transform.position = p;
                        velocity = Vector3.zero;
                        _softReturningToPlayer = false;
                    }
                }

                return;
            }

            // Hard lock: exact position every frame (no lag)
            _softReturningToPlayer = false;
            transform.position = p;
            return;
        }


        // FREE MODE: Edge Panning (same logic, Input System mouse)
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        Vector3 targetPos = transform.position;

        float speed = panSpeed * unlockedPanSpeedMult;

        bool inEdgeZone =
            mousePos.y >= Screen.height - panBorderThickness ||
            mousePos.y <= panBorderThickness ||
            mousePos.x >= Screen.width - panBorderThickness ||
            mousePos.x <= panBorderThickness;

        if (inEdgeZone)
            speed *= edgePanSpeedMult;

        float delta = speed * Time.deltaTime;

        if (mousePos.y >= Screen.height - panBorderThickness) targetPos.y += delta;
        if (mousePos.y <= panBorderThickness) targetPos.y -= delta;
        if (mousePos.x >= Screen.width - panBorderThickness) targetPos.x += delta;
        if (mousePos.x <= panBorderThickness) targetPos.x -= delta;

        targetPos.z = -10f;

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    void HandleZoom()
    {
        if (cam == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y * scrollScale;

        if (scroll != 0)
        {
            targetZoom -= scroll * 1000f * Time.deltaTime; // keep your original math
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }
}
