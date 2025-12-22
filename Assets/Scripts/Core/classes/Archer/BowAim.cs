using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class BowAim2D : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform center;
    [SerializeField] private Camera ownerCamera;

    [Tooltip("If set, bow flip will follow this PlayerController's FacingLeft (recommended).")]
    [SerializeField] private PlayerController player;

    [Header("Orbit")]
    [SerializeField] private float radius = 0.45f;
    [SerializeField] private Vector2 localOffset = Vector2.zero;

    [Tooltip("Shift the orbit circle center sideways. Facing right uses +X, facing left uses -X.")]
    [SerializeField] private float orbitCenterShiftX = 0.20f;

    [Header("Sprite / Art Alignment")]
    [Tooltip("Your bow art points down by default.")]
    [SerializeField] private float spriteAimOffsetDeg = -45f;

    [Header("Flip")]
    [Tooltip("If your bow looks mirrored the wrong way, invert this.")]
    [SerializeField] private bool invertFlip = false;

    [Header("Networking")]
    [SerializeField] private float sendRateHz = 20f;

    private readonly NetworkVariable<float> aimAngleDeg = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float sendTimer;

    private void Awake()
    {
        if (center == null) center = transform.root;
    }

    public override void OnNetworkSpawn()
    {
        if (player == null)
            player = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        if (center == null) return;

        if (IsOwner)
        {
            if (ownerCamera == null)
                ownerCamera = GetComponentInParent<PlayerController>()?.GetComponentInChildren<Camera>(true);

            if (ownerCamera == null) return;

            Vector2 mouseWorld = ownerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 dir = mouseWorld - (Vector2)center.position;
            if (dir.sqrMagnitude < 0.0001f) return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            sendTimer -= Time.deltaTime;
            if (sendTimer <= 0f)
            {
                sendTimer = 1f / Mathf.Max(1f, sendRateHz);
                SubmitAimServerRpc(angle);
            }

            ApplyOrbitRotationAndFlip(angle);
        }
        else
        {
            ApplyOrbitRotationAndFlip(aimAngleDeg.Value);
        }
    }

    private void ApplyOrbitRotationAndFlip(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;

        // Flip should match the BODY facing, not recomputed locally.
        bool facingLeft = false;
        if (player != null)
            facingLeft = player.FacingLeft.Value;

        bool doFlip = facingLeft;
        if (invertFlip) doFlip = !doFlip;

        // Shift orbit circle center left/right depending on facing
        float shift = doFlip ? -orbitCenterShiftX : orbitCenterShiftX;

        // Orbit position around shifted center
        Vector2 orbit = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        Vector2 shiftedCenter = (Vector2)center.position + new Vector2(shift, 0f);
        transform.position = shiftedCenter + orbit + localOffset;

        // Apply flip on X scale
        Vector3 s = transform.localScale;
        float absX = Mathf.Abs(s.x);
        s.x = doFlip ? -absX : absX;
        transform.localScale = s;

        // Rotation
        float z = doFlip
            ? (angleDeg - 180f - spriteAimOffsetDeg)
            : (angleDeg + spriteAimOffsetDeg);

        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    [ServerRpc(RequireOwnership = true)]
    private void SubmitAimServerRpc(float angleDeg)
    {
        aimAngleDeg.Value = angleDeg;
    }
}
