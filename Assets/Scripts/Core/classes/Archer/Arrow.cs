using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : NetworkBehaviour
{
    [SerializeField] private float lifetimeSeconds = 3f;
    [SerializeField] private float spriteAngleOffsetDeg = 0f; // adjust if sprite points “up” by default
    [SerializeField] private float knockbackImpulse = 6f;


    private Rigidbody2D rb;
    private Collider2D col;

    private int damage;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Projectiles should generally be triggers to avoid bounces.
        if (col != null) col.isTrigger = true;
    }

    public override void OnNetworkSpawn()
    {
        // Only server simulates + collides. Clients only render replicated transform.
        if (!IsServer)
        {
            if (rb != null) rb.simulated = false;
            if (col != null) col.enabled = false;
            return;
        }

        if (rb != null) rb.gravityScale = 0f;
        Invoke(nameof(ServerDespawn), lifetimeSeconds);
    }

    /// Server-only init. Also ignores collisions with shooter colliders.
    public void ServerInit(Vector2 dir, float speed, int dmg, ulong shooterNetworkObjectId)
    {
        if (!IsServer) return;

        damage = dmg;
        initialized = true;

        // Ignore collisions with shooter so you never hit yourself at spawn.
        IgnoreShooterCollisions(shooterNetworkObjectId);

        dir = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;

        if (rb != null)
            rb.linearVelocity = dir * speed;

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

        // Damage
        var s = other.GetComponent<stats>();
        if (s != null)
            s.takeDamage(damage);

        // Knockback 
        var targetRb = other.attachedRigidbody; // works even if collider is on a child
        if (targetRb != null)
        {
            Vector2 kbDir =
                (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
                    ? rb.linearVelocity.normalized
                    : ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

            targetRb.AddForce(kbDir * knockbackImpulse, ForceMode2D.Impulse);
        }

        // Despawn projectile
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
