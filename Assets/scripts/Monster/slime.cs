using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class slime : MonoBehaviour

{
    private float crawlSpeed = 2.0f;      // Speed at which the slime crawls
    private float jumpForce = 5.0f;       // Force of the jump towards the player
    private float detectionRange = 50.0f; // Range within which the slime detects the player
    private float jumpRange = 2.0f;       // Range within which the slime will jump towards the player
    private float jumpCooldown = 3.0f;    // Cooldown time for jumping in seconds

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

        // If the player is within detection range, start crawling towards them
        if (distanceToPlayer < detectionRange && distanceToPlayer > jumpRange)
        {
            SlimeCrawl();
        }

        // If the player is within jump range and the cooldown has elapsed, perform the jump
        if (distanceToPlayer < jumpRange && Time.time >= lastJumpTime + jumpCooldown)
        {
            SlimeJump();
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

    void SlimeCrawl()
    {
        // Play the crawl animation
        animator.Play("slimeCrawl");

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

    void SlimeJump()
    {
        // Play the jump animation
        animator.Play("slimeJump");

        // Calculate the jump direction and apply force
        Vector2 jumpDirection = (player.position - transform.position).normalized;
        rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);

        // Update the last jump time
        lastJumpTime = Time.time;

        // Flip the sprite based on the direction
        if (jumpDirection.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (jumpDirection.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }
}
