using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public GameObject Menu;


    void Start()
    {

    }
    public virtual void toggleMenu()
    {
        if (Menu != null)
        {
            if (Menu.activeSelf)
            {
                Menu.SetActive(false);
            }
            else
            {
                Menu.SetActive(true);
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

    }
    public virtual void closeMenu()
    {
        Menu.SetActive(false);

    }

    public virtual void switchToMenu(GameObject InactiveMenu)
    {
        Menu.SetActive(false);
        InactiveMenu.SetActive(true);
    }

}
