using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    public stats stats;
    public float speed;
    public Animator animator;
    

    void Start()
    {
        speed = stats.speed;
    }


    void Update()
    {
        
        Vector3 pos = transform.position;

        if (Input.GetKey(KeyCode.W))
        {
            pos.y += speed * Time.deltaTime;
            //animator.SetBool("run", true);
        }

        if (Input.GetKey(KeyCode.S))
        {
            pos.y -= speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            GetComponent<SpriteRenderer>().flipX = false;
            pos.x += speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            GetComponent<SpriteRenderer>().flipX = true;
            pos.x -= speed * Time.deltaTime;
        }

        transform.position = pos;
        
        
        
    }


}