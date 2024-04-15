using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Psychic : CharacterClass
{
    public Psychic()
    {
        CharacterClassName = "Psychic";
    }

    public void Grab()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the ray hits an object with the tag "Enemy"
            if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Enemy"))
            {
                // If so, do something (e.g., grab the enemy)
                Debug.Log("Psychics grabbed the enemy!");
            }
        }
    }
}


