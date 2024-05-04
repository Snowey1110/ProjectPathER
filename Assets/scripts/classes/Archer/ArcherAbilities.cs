using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherAbilities : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator animator;

    //Dash
    public int dashLvl = 5;
    private bool dashCD = false;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !dashCD)
        {
            if (dashLvl == 0){
                Debug.Log("Ability hasn't unlocked");
            } else if (dashLvl == 1)
            {
                Dash(10, 30, 0.15f);
            } else if (dashLvl == 2)
            {
                Dash(5, 30, 0.15f);
            } else if (dashLvl == 3)
            {
                Dash(3, 35, 0.15f);
            } else if (dashLvl == 4)
            {
                Dash(1.5f, 40, 0.15f);
            } else if (dashLvl >= 5)
            {
                Dash(1, 40, 0.15f);
            }


    }
    }

    void Dash(float cooldown, float dashDistance, float dashTime)
    {
        // Check if there's a direction to dash towards
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dashDirection = (mousePosition - (Vector2)transform.position).normalized;
        if (dashDirection != Vector2.zero)
        {
            // Disable ability during cooldown
            dashCD = true;
            Invoke("ResetCooldown", cooldown);


            // Perform dash
            StartCoroutine(PerformDash(dashDirection, dashDistance, dashTime));
        }
    }

    IEnumerator PerformDash(Vector2 direction, float dashDistance, float dashDuration)
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
        dashCD = false;
    }
}
