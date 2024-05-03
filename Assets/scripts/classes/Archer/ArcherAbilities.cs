using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherAbilities : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator animator;

    //Dash
    public int dash = 5;
    public float dashDistance = 30f; // Distance to dash
    public float dashDuration = 0.5f; // Duration of the dash
    public float cooldown = 1f; // Cooldown of the ability
    private bool isOnCooldown = false;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isOnCooldown)
        {
            Dash();
        }
    }

    void Dash()
    {
        // Check if there's a direction to dash towards
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dashDirection = (mousePosition - (Vector2)transform.position).normalized;
        if (dashDirection != Vector2.zero)
        {
            // Disable ability during cooldown
            isOnCooldown = true;
            Invoke("ResetCooldown", cooldown);


            // Perform dash
            StartCoroutine(PerformDash(dashDirection));
        }
    }

    IEnumerator PerformDash(Vector2 direction)
    {
        float elapsedTime = 0;

        // Loop until dash duration is reached
        while (elapsedTime < dashDuration)
        {
            // Move the character in the dash direction
            transform.Translate(direction * dashDistance * Time.deltaTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    void ResetCooldown()
    {
        isOnCooldown = false;
    }
}
