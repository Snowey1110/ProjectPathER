using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : NetworkBehaviour
{
    [SerializeField] private float lifetimeSeconds = 3f;

    private Rigidbody2D rb;
    private int damage;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        // Server simulates; clients render replicated motion.
        if (!IsServer)
        {
            rb.simulated = false;
            return;
        }

        rb.simulated = true;

        // Optional but usually desirable for projectiles:
        rb.gravityScale = 0f;

        Invoke(nameof(DespawnSelf), lifetimeSeconds);
    }

    // Called by ArcherAttack on the server right after Spawn()
    public void ServerInit(Vector2 dir, float speed, int dmg)
    {
        if (!IsServer) return;

        damage = dmg;
        initialized = true;

        rb.linearVelocity = dir.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
        if (!IsServer) return;
        if (!initialized) return;
        if (other == null) return;

        // disable friendly fire here
        // if (other.CompareTag("Player")) return;

        // If it has stats, treat it as damageable (works for Untagged Slime).
        stats s = other.GetComponent<stats>();
        if (s != null)
        {
            s.takeDamage(damage);
            DespawnSelf();
            return;
        }

        // Otherwise, hit world/props: still despawn (prevents bouncing forever).
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
