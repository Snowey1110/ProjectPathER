using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float destroyTime = 3f;
    bool hasHit;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    // FixedUpdate for better physics manipulation
    void FixedUpdate()
    {
        if (hasHit)
            return;

        // Make the arrow face its move direction
        float angle = Mathf.Atan2(GetComponent<Rigidbody2D>().velocity.y, GetComponent<Rigidbody2D>().velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
       
    }


    void OnCollisionEnter2D(Collision2D collision)
    {

        hasHit = true;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero; // Stop moving after hit
        GetComponent<Rigidbody2D>().isKinematic = true; // Stop physics after hit
        Destroy(collision.gameObject);
        Destroy(transform.gameObject);
    }
}