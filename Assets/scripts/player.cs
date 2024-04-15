using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    public stats stats;
    public float speed;
    public CharacterClass characterClass; 
    

    void Start()
    {
        speed = stats.speed;
        characterClass = new Psychic();
        
        Debug.Log(characterClass.getName());
    }


    void Update()
    {
        
        Vector3 pos = transform.position;

        if (Input.GetKey(KeyCode.W))
        {
            pos.z += speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            pos.z -= speed * Time.deltaTime;
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