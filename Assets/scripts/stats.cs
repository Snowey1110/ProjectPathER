using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class stats : MonoBehaviour
{
    public HealthBar healthBar;
    public Player player;


    public int HP = 10;
    public int currentHealth;
    public int defense = 1;
    public int mana = 10;
    public int baseDamage = 2;
    public int level = 0;
    public int abilityPoints = 0;




    public bool allowCombat = true;

    // Checking if the unit should be dead
    private void Start()
    {
        currentHealth = HP;
        healthBar.SetMaxHealth(HP);

        //GameObject player = GameObject.FindWithTag("Player");
        //float speed = player.GetComponent<Player>().speed;



    }
    private void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentHealth -= 1;
            healthBar.SetHealth(currentHealth);
        }
    }

    public void takeDamage(int damageReceived)
    {
        currentHealth = currentHealth - damageReceived;
        healthBar.SetHealth(currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
        Debug.Log("Unit died.");

    }




}
