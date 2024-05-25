using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public GameObject Menu;
    public PauseManager SettingMenu;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Menu.activeSelf)
            {
                Menu.SetActive(false);
                SettingMenu.UnpauseGame();
            }
        }
    }
    public virtual void toggleMenu()
    {
        if (Menu != null)
        {
            if (Menu.activeSelf)
            {
                Menu.SetActive(false);
                SettingMenu.UnpauseGame();
            }
            else
            {
                Menu.SetActive(true);
                SettingMenu.PauseGame();
            }
        }
        else
        {
            Debug.LogWarning("Menu GameObject is not assigned!");
        }
    }
    public virtual void openMenu()
    {
        Menu.SetActive(true);
        SettingMenu.PauseGame();

    }
    public virtual void closeMenu()
    {
        Menu.SetActive(false);
        SettingMenu.UnpauseGame();
    }

    public virtual void switchToMenu(GameObject InactiveMenu)
    {
        Menu.SetActive(false);
        InactiveMenu.SetActive(true);
    }

}
