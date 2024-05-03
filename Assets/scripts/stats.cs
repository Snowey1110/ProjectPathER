using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class stats : MonoBehaviour
{
    public int HP = 10;
    public int defense = 1;
    public int mana = 10;
    public int baseDamage = 1;
    public int level = 0;

    // Checking if the unit should be dead
    private void Update()
    {
        if (HP <= 0)
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
