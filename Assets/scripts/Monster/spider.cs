using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderMonsterScript : MonoBehaviour
{
    public float moveSpeed; // the speed at which the spider monster will move towards the player
    private GameObject player;
    private Rigidbody2D rb;
    private Animator animator;
    public string playerTag = "Player";
    public float chaseRange = 10f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag(playerTag);
    }

    void Update()
    {
        if (player != null)
        {
            Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;

            // if player is close to the spider monster
            if (Vector2.Distance(player.transform.position, transform.position) < chaseRange)
            {
                // move towards the player
                rb.MovePosition((Vector2)transform.position + (direction * moveSpeed * Time.deltaTime));
                RotateTowardsPlayer();

                if (!animator.GetBool("isWalking"))
                {
                    animator.SetBool("isWalking", true);
                }

                // Set idle animation to false
                if (animator.GetBool("isIdle"))
                {
                    animator.SetBool("isIdle", false);
                }
            }
            else
            {
                // Set walking animation to false
                if (animator.GetBool("isWalking"))
                {
                    animator.SetBool("isWalking", false);
                }

                // Play idle animation
                if (!animator.GetBool("isIdle"))
                {
                    animator.SetBool("isIdle", true);
                }
            }
        }
        else
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
        }
    }

    private void RotateTowardsPlayer()
    {
        Vector2 direction = player.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        rb.rotation = angle;
    }
}