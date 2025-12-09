using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
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
        if (!IsOwner) return;
        Vector3 moveDir = Vector3.zero;
        bool isWalking = false;

        if (Input.GetKey(KeyCode.W))
        {
            moveDir.y += 1;
            isWalking = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            moveDir.y -= 1;
            isWalking = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            if (animator.GetBool("attacking") != true)
            {
                GetComponent<SpriteRenderer>().flipX = false;
            }
            
            moveDir.x += 1;
            isWalking = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            if (animator.GetBool("attacking") != true)
            {
                GetComponent<SpriteRenderer>().flipX = true;
            }
            
            moveDir.x -= 1;
            isWalking = true;
        }

        if (isWalking)
        {
            // Normalize the move direction vector to account for diagonal movement
            moveDir.Normalize();
            transform.position += moveDir * speed * Time.deltaTime;
        }

        animator.SetBool("walking", isWalking);
    }

    public override void OnNetworkSpawn()
    {
        // Only run this if *I* am the one controlling this character
        if (IsOwner)
        {
            // Find the camera in the scene
            GameObject cam = GameObject.FindWithTag("MainCamera");

            if (cam != null)
            {
                // Tell the camera: "Hey, follow ME!"
                cam.GetComponent<CameraController>().player = this.gameObject;
            }
        }
    }
}