using Unity.Netcode;
using UnityEngine;
using static PlayerController;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : NetworkBehaviour
{
    [SerializeField] private float lifetimeSeconds = 3f;
    [SerializeField] private float spriteAngleOffsetDeg = 0f;

    private Rigidbody2D rb;
    private Collider2D col;

    private int damage;
    private bool initialized;
    private bool friendlyFire;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Make projectile pass-through and avoid physics bounce
        if (col != null) col.isTrigger = true;

        // Prevent solver torque spin
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public override void OnNetworkSpawn()
    {
        // Only server simulates + collides
        if (!IsServer)
        {
            if (rb != null) rb.simulated = false;
            if (col != null) col.enabled = false;
            enabled = false;
            return;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.angularVelocity = 0f;
        }

        Invoke(nameof(ServerDespawn), lifetimeSeconds);
    }

    public void ServerInit(Vector2 dir, float speed, int dmg, ulong shooterNetworkObjectId, bool shooterFriendlyFire)
    {
        if (!IsServer) return;

        damage = dmg;
        initialized = true;
        friendlyFire = shooterFriendlyFire;

        // Temporarily disable collider while setting ignore rules to avoid immediate self-hit at spawn
        if (col != null) col.enabled = false;

        IgnoreShooterCollisions(shooterNetworkObjectId);

        Physics2D.SyncTransforms();
        if (col != null) col.enabled = true;

        dir = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;

        rb.linearVelocity = dir * speed;
        rb.angularVelocity = 0f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngleOffsetDeg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void IgnoreShooterCollisions(ulong shooterNetworkObjectId)
    {
        if (col == null) return;
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterNetworkObjectId, out var shooterNo))
            return;

        foreach (var sc in shooterNo.GetComponentsInChildren<Collider2D>(true))
        {
            if (sc == null) continue;
            Physics2D.IgnoreCollision(col, sc, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || !initialized) return;
        if (other == null) return;

        // --- Player filtering rules ---
        var targetPC = other.GetComponentInParent<PlayerController>();
        if (targetPC != null)
        {
            // Ghost is never hittable
            if (targetPC.Class.Value == PlayerClass.Ghost) return;

            // Always ignore Archer class
            //if (targetPC.Class.Value == PlayerClass.Archer) return;

            // Friendly fire OFF: ignore other player classes
            if (!friendlyFire) return;

            // Friendly fire ON: hit Knight/Mage/Healer
            var targetStats = targetPC.GetComponent<stats>();
            if (targetStats != null)
                targetStats.takeDamage(damage);

            ServerDespawn();
            return;
        }

        // --- Non-player targets (monsters, props) ---
        var s = other.GetComponent<stats>();
        if (s != null)
        {
            s.takeDamage(damage);
            ServerDespawn();
        }
        else
        {
            // Despawn on walls etc.
            ServerDespawn();
        }
    }

    private void ServerDespawn()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
