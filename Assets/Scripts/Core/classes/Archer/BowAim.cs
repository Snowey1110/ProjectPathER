using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class BowAim2D : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bowTransform;          // usually this.transform
    [SerializeField] private Transform playerRoot;            // the player (ClassArcher root)
    [SerializeField] private Camera ownerCamera;              // if null we find in PlayerController
    [SerializeField] private SpriteRenderer bodySprite;       // for flip logic (optional)

    [Header("Net")]
    [SerializeField] private float sendRateHz = 20f;

    private readonly NetworkVariable<float> aimAngleDeg = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float sendTimer;

    private void Awake()
    {
        if (bowTransform == null) bowTransform = transform;
        if (playerRoot == null) playerRoot = transform.root;
    }

    public override void OnNetworkSpawn()
    {
        // Remote clients only need to render from replicated aimAngleDeg.
        // Owner will send updates.
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            // Find owner camera if not assigned
            if (ownerCamera == null)
                ownerCamera = GetComponentInParent<PlayerController>()?.GetComponentInChildren<Camera>(true);

            if (ownerCamera == null || playerRoot == null) return;

            sendTimer -= Time.deltaTime;
            if (sendTimer <= 0f)
            {
                sendTimer = 1f / Mathf.Max(1f, sendRateHz);

                Vector2 mouseWorld = ownerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 dir = (mouseWorld - (Vector2)playerRoot.position);
                if (dir.sqrMagnitude < 0.0001f) return;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                SubmitAimServerRpc(angle);

                // also apply immediately for the owner (no waiting for replication)
                ApplyBowVisual(angle);
            }
        }
        else
        {
            ApplyBowVisual(aimAngleDeg.Value);
        }
    }

    private void ApplyBowVisual(float angleDeg)
    {
        // Rotate bow in Z
        bowTransform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

        // If body flips, mirror the bow so it doesn’t look upside-down.
        if (bodySprite != null)
        {
            Vector3 s = bowTransform.localScale;
            s.y = bodySprite.flipX ? -Mathf.Abs(s.y) : Mathf.Abs(s.y);
            bowTransform.localScale = s;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SubmitAimServerRpc(float angleDeg)
    {
        aimAngleDeg.Value = angleDeg;
    }
}
