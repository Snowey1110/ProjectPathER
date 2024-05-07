using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class stats : MonoBehaviour
{
    public HealthBar healthBar;
    public Player player;


    public int HP;
    public int currentHealth;
    public int defense;
    public int mana;
    public int baseDamage;
    public int level;
    public int abilityPoints;
    public int totalAbilityPoints;



    public bool allowCombat = true;

    // Checking if the unit should be dead
    private void Start()
    {
        
        currentHealth = HP;
        healthBar.SetMaxHealth(HP, currentHealth);
        totalAbilityPoints = abilityPoints;
        //GameObject player = GameObject.FindWithTag("Player");
        //float speed = player.GetComponent<Player>().speed;



    }
    private void Update()
    {
        
        //Test function, hit space to take 1 damage
        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            takeDamage(1);
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
