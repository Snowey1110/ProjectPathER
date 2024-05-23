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

    /*
    //Shoot
    public GameObject arrowPrefab;
    public float chargingSpeed = 10f; //incremental multiplier
    public float maxCharge = 10f; //max multiplier
    public float shootForce = 10; //increased base shoot force
    private float currentCharge = 1f;
    private Vector3 shootDir;
    private bool isCharging = false;
    public GameObject aimIndicator;
    */

    //New shoot
    public bool attacking;
    public GameObject arrowPrefab;
    public float shootForce = 10f; 
    public float shootCooldown = 2f;
    private float nextShootTime = 0f;
    private Vector3 shootDir;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*
        //Shoot
        if (Input.GetMouseButtonDown(0) && GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat)
        {
            isCharging = true;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            aimIndicator.transform.position = mousePos;
            aimIndicator.SetActive(true);
        }

        if (isCharging)
        {
            currentCharge += chargingSpeed * Time.deltaTime;
            if (currentCharge > maxCharge)
            {
                currentCharge = maxCharge;
            }
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            isCharging = false;
            aimIndicator.SetActive(false);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            shootDir = (mousePos - transform.position).normalized;
            GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity) as GameObject;
            Rigidbody2D rb2d = arrow.GetComponent<Rigidbody2D>();
            rb2d.AddForce(shootDir * shootForce * currentCharge);
            currentCharge = 1f; //reset the current charge after firing
        }
        */

        //New Shoot
        

        if (Input.GetMouseButton(0) && Time.time >= nextShootTime)
        {
            attacking = true;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;


            shootDir = (mousePos - transform.position).normalized;
            ShootArrow();

            nextShootTime = Time.time + shootCooldown; // set the next shoot time
        }

        if (!Input.GetMouseButton(0))
        {
            attacking = false;
        }
        animator.SetBool("attacking", attacking);


        //Dash
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

            // Flip sprite depending on dash direction
            if (dashDirection.x < 0)
            {
                GetComponent<SpriteRenderer>().flipX = true;
            }
            else
            {
                GetComponent<SpriteRenderer>().flipX = false;
            }

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

    void ShootArrow()
    {
        GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
        Rigidbody2D rb2d = arrow.GetComponent<Rigidbody2D>();
        rb2d.AddForce(shootDir * shootForce, ForceMode2D.Impulse);
    }
}
