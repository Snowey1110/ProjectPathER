using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class ClassSelect : MonoBehaviour
{
    public GameObject Archer;
    public GameObject Knight;
    public GameObject Mage;
    public GameObject Phychics;
    public Camera MainCamera;
    public void selectArcher()
    {
        GameObject newArcher = Instantiate(Archer, new Vector3(0, 0, 0), Quaternion.identity);
        MainCamera.GetComponent<CameraController>().player = newArcher;
        Destroy(gameObject);
    }




}
