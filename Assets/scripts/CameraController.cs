
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float panSpeed = 10f;
    private float panBorderThickness = 10f;
    private float zoomSpeed = 1000f;
    private float minZoom = 1f;
    private float maxZoom = 20f;
    public bool controlWithMouse = true;

    public Camera cam;
    public GameObject player;

    void Update()
    {
        Vector3 pos = transform.position;

        //Center the camera to player on update
        if (Input.mousePosition.y <= Screen.height - panBorderThickness && Input.mousePosition.y >= panBorderThickness && Input.mousePosition.x >= panBorderThickness && Input.mousePosition.x <= Screen.width - panBorderThickness)
        {
            pos = player.transform.position + new Vector3(0, 0, -10);
        }

        //Control the camera when mouse is off screen
        if (controlWithMouse == true)
        {

            if (Input.mousePosition.y >= Screen.height - panBorderThickness)
            {
                pos.y += panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.y <= panBorderThickness)
            {
                pos.y -= panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x <= panBorderThickness)
            {
                pos.x -= panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x >= Screen.width - panBorderThickness)
            {
                pos.x += panSpeed * Time.deltaTime;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Resetting camera");
            pos = player.transform.position + new Vector3(0, 10, 0);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float newZoom = cam.orthographicSize - scroll * zoomSpeed * Time.deltaTime;
        cam.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);

        transform.position = pos;
    }
}