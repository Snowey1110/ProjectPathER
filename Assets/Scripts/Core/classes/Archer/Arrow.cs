using Unity.Netcode;
using UnityEngine;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // For projectiles: use trigger to avoid bounce responses
        if (col != null) col.isTrigger = true;

        // Prevent any physics torque spin
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public override void OnNetworkSpawn()
    {
        // Clients should not simulate or collide. Also disable this script to prevent any client-side rotation logic.
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

    public void ServerInit(Vector2 dir, float speed, int dmg, ulong shooterNetworkObjectId)
    {
        if (!IsServer) return;

        damage = dmg;
        initialized = true;

        // Disable collider until we're done ignoring shooter collisions
        if (col != null) col.enabled = false;

        IgnoreShooterCollisions(shooterNetworkObjectId);

        // Ensure transforms are up-to-date before enabling collider
        Physics2D.SyncTransforms();

        if (col != null) col.enabled = true;

        dir = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;

        // Velocity + rotation
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

        var shooterCols = shooterNo.GetComponentsInChildren<Collider2D>(true);
        foreach (var sc in shooterCols)
        {
            if (sc == null) continue;
            Physics2D.IgnoreCollision(col, sc, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || !initialized) return;
        if (other == null) return;

        if (other.CompareTag("Player")) return;

        var s = other.GetComponent<stats>();
        if (s != null)
            s.takeDamage(damage);

        ServerDespawn();
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
