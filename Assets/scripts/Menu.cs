using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public GameObject Menu;
    public GameObject smb; //Sound Menu

    void Start()
    {
        /*
        GameObject smb = GameObject.Find("Sound Menu Background");
        smb.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
        */
        if (smb != null)
        {
            smb.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
        }





    }
    public void toggleMenu()
    {
        if (Menu != null)
        {
            if (Menu.activeSelf)
            {
                Menu.SetActive(false);
                if (GameObject.FindWithTag("Player") != null)
                {
                    GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = true;
                }
            }
            else
            {
                Menu.SetActive(true);
                if (GameObject.FindWithTag("Player") != null)
                {
                    GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = true;
                }
            }
        }
        else
        {
            Debug.LogWarning("Menu GameObject is not assigned!");
        }
    }
    public void openMenu()
    {
        Menu.SetActive(true);
    }
    public void closeMenu()
    {
        Menu.SetActive(false);
    }


}
