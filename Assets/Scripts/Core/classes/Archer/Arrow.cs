using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : NetworkBehaviour
{
    [SerializeField] private float lifetimeSeconds = 3f;

    // If your arrow sprite points �up� by default, set this to +90 or -90 in inspector.
    [SerializeField] private float spriteAngleOffsetDeg = 0f;

    private Rigidbody2D rb;
    private int damage;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Clients should NOT run projectile physics/collision
            if (rb != null) rb.simulated = false;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            return;
        }

        Invoke(nameof(ServerDespawn), lifetimeSeconds);
    }

    public void ServerInit(Vector2 dir, float speed, int dmg)
    {
        if (!IsServer) return;

        damage = dmg;
        initialized = true;

        // Set velocity
        rb.linearVelocity = dir.normalized * speed;

        // Set rotation to face travel direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngleOffsetDeg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleHit(other);
    private void OnCollisionEnter2D(Collision2D collision) => HandleHit(collision.collider);

    private void HandleHit(Collider2D other)
    {
        if (!IsServer || !initialized || other == null) return;
        if (other.CompareTag("Player")) return; // ignore players

        // Damage if the target has stats (works even if enemy isn't tagged "Enemy")
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
