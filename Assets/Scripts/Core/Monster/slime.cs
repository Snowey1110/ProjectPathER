using Unity.Netcode;
using UnityEngine;

public class Slime : NetworkBehaviour
{
    [Header("AI Ranges")]
    [SerializeField] private float detectionRange = 50f;
    [SerializeField] private float jumpRange = 10f;

    [Header("Speeds")]
    [SerializeField] private float crawlSpeed = 2f;
    [SerializeField] private float jumpCrawlSpeed = 8f;

    [Header("Combat")]
    [SerializeField] private float knockbackForce = 6f;

    private bool jumpCD;
    private Transform targetPlayer;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    public override void OnNetworkSpawn()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Only the server runs AI + applies damage.
        if (!IsServer)
            return;

        RegisterAnyPlayer();
    }

    private void Update()
    {
        if (!IsServer) return;

        if (targetPlayer == null)
        {
            RegisterAnyPlayer();
            return;
        }

        float dist = Vector2.Distance(transform.position, targetPlayer.position);

        if (dist < jumpRange && !jumpCD)
        {
            StartJump();
        }

        bool jumping = animator != null && animator.GetBool("jumping");

        if (dist < detectionRange && !jumping)
            MoveToward(targetPlayer, crawlSpeed);

        if (dist < detectionRange && jumping)
            MoveToward(targetPlayer, jumpCrawlSpeed);
    }

    private void RegisterAnyPlayer()
    {
        // In NGO, prefer connected clients' PlayerObject instead of FindWithTag.
        if (NetworkManager.Singleton == null) return;

        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (c?.PlayerObject != null)
            {
                targetPlayer = c.PlayerObject.transform;
                return;
            }
        }

        targetPlayer = null;
    }

    private void MoveToward(Transform player, float speed)
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // Server-authoritative movement.
        transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

        // Flip (server drives; clients see via animator/sprite state if you replicate, otherwise acceptable as cosmetic)
        if (spriteRenderer != null)
        {
            if (dir.x > 0) spriteRenderer.flipX = false;
            else if (dir.x < 0) spriteRenderer.flipX = true;
        }
    }

    private void StartJump()
    {
        if (animator != null) animator.SetBool("jumping", true);
        jumpCD = true;

        // Cooldown reset on server
        Invoke(nameof(ResetCooldown), Random.Range(2, 5));
    }

    private void ResetCooldown()
    {
        jumpCD = false;
    }

    // Called by animation event if you have one.
    public void jumpingFalse()
    {
        if (!IsServer) return;
        if (animator != null) animator.SetBool("jumping", false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Only damage real player objects (spawned player characters), not lobby ghosts.
        var pc = collision.gameObject.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        // Deal damage server-side only
        stats playerStats = pc.GetComponent<stats>();
        stats slimeStats = GetComponent<stats>();

        if (playerStats != null && slimeStats != null)
            playerStats.takeDamage(slimeStats.baseDamage);

        // Knockback slime away from player (server)
        if (rb != null)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
            rb.AddForce(away * knockbackForce, ForceMode2D.Impulse);
        }
    }
}
