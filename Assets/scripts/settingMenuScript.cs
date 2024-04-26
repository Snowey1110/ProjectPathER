using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class settingMenuScript : MonoBehaviour
{
    public GameObject settingMenu;

    void Start()
    {
        GameObject smb = GameObject.Find("Sound Menu Background");
        smb.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
    }
    public void toggleSoundController()
    {
        if (settingMenu != null)
        {
            if (settingMenu.activeSelf)
            {
                settingMenu.SetActive(false);
            }
            else
            {
                settingMenu.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("settingMenu GameObject is not assigned!");
        }
    }
    public void openSoundController()
    {
        settingMenu.SetActive(true);
    }
    public void closeSoundController()
    {
        settingMenu.SetActive(false);
    }
}
