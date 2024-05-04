using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class stats : MonoBehaviour
{
    public int HP = 10;
    public int currentHealth;
    public int defense = 1;
    public int mana = 10;
    public int baseDamage = 1;
    public int level = 0;
    public HealthBar healthBar;

    // Checking if the unit should be dead
    private void Start()
    {
        currentHealth = HP;
        healthBar.SetMaxHealth(HP);
    }
    private void Update()
    {
        if (currentHealth <= 0)
        {
            Die();
        }
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
    }

    private void Die()
    {
        Destroy(gameObject);
        Debug.Log("Unit died.");

    }


}
