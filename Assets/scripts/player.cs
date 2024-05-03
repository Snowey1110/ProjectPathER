using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    public stats stats;
    public float speed;
    public Animator animator;
    

    void Start()
    {

    }


    void Update()
    {

        Vector3 pos = transform.position;
        bool isWalking = false;

        if (Input.GetKey(KeyCode.W))
        {
            pos.y += speed * Time.deltaTime;
            isWalking = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            pos.y -= speed * Time.deltaTime;
            isWalking = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            GetComponent<SpriteRenderer>().flipX = false;
            pos.x += speed * Time.deltaTime;
            isWalking = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            GetComponent<SpriteRenderer>().flipX = true;
            pos.x -= speed * Time.deltaTime;
            isWalking = true;
        }

        animator.SetBool("walking", isWalking);
        transform.position = pos;



    }


}