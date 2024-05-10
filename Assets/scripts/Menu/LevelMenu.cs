using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelMenu : MenuToggle
{
    public statsMenuController statsMenuController;
    public override void toggleMenu()
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
                    GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = false;
                }
            }
        }
        else
        {
            Debug.LogWarning("Menu GameObject is not assigned!");
        }
        statsMenuController.confirmAbilityPointSpent();
    }
    public override void openMenu()
    {
        Menu.SetActive(true);

    }
    public override void closeMenu()
    {
        Menu.SetActive(false);
        if (GameObject.FindWithTag("Player") != null)
        {
            GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = true;
        }
        statsMenuController.confirmAbilityPointSpent(); 
    }

    public override void switchToMenu(GameObject InactiveMenu)
    {
        Menu.SetActive(false);
        InactiveMenu.SetActive(true);
        if (GameObject.FindWithTag("Player") != null)
        {
            GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = false;
        }
        statsMenuController.confirmAbilityPointSpent();
    }
}
