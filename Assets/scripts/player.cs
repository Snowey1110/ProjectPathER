using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    public stats stats;
    public float speed;
    

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
        }

        if (Input.GetKey(KeyCode.S))
        {
            pos.y -= speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            pos.x += speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            pos.x -= speed * Time.deltaTime;
        }

        transform.position = pos;
        
        
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(collision);
    }

}