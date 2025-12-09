using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slime : MonoBehaviour
{

    private float detectionRange = 50.0f; // Range within which the slime detects the player
    private float jumpRange = 10.0f;       // Range within which the slime will jump towards the player
    private bool jumpCD = false;
    private Transform player;            // Reference to the player's transform
    private Animator animator;           // Reference to the Animator component
    private Rigidbody2D rb;              // Reference to the Rigidbody2D component
    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component

    private float lastJumpTime = -Mathf.Infinity; // Tracks the time of the last jump

    void Start()
    {
        // Try to find the player in the scene
        RegisterPlayer();

        // Get the Animator component attached to the slime
        animator = GetComponent<Animator>();

        // Get the Rigidbody2D component attached to the slime
        rb = GetComponent<Rigidbody2D>();

        // Get the SpriteRenderer component attached to the slime
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // If no player is found, try to find the player
        if (player == null)
        {
            RegisterPlayer();
            return;
        }

        // Calculate the distance to the player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // If the player is within jump range and the cooldown has elapsed, perform the jump
        if (distanceToPlayer < jumpRange && jumpCD == false)
        {
            jumpingTrue();
            
        }

        // If the player is within detection range and not jumping, start crawling towards them
        if (distanceToPlayer < detectionRange && !animator.GetBool("jumping"))
        {
            SlimeMove(2);
        }

        if (distanceToPlayer < detectionRange && animator.GetBool("jumping"))
        {
            SlimeMove(8);
        }
    }

    void RegisterPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void SlimeMove(float crawlSpeed)
    {
        // Move towards the player
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, crawlSpeed * Time.deltaTime);

        // Flip the sprite based on the direction
        if (direction.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (direction.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }


    public void jumpingTrue()
    {
        animator.SetBool("jumping", true);
        jumpCD = true;
        Invoke("ResetCooldown", Random.Range(2, 5));
    }

    void ResetCooldown()
    {
        jumpCD = false;
    }
    public void jumpingFalse()
    {
        animator.SetBool("jumping", false);
    }


    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Deal damage to the player
            stats playerStats = collision.gameObject.GetComponent<stats>();
            int slimeAttackStats = gameObject.GetComponent<stats>().baseDamage;
            playerStats.takeDamage(slimeAttackStats);

            // Knockback effect
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
            float knockbackForce = 100.0f; 
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }
    }

}
