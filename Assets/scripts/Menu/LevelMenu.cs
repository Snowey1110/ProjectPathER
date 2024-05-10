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
                if (statsMenuController != null)
                {
                    statsMenuController.confirmAbilityPointSpent();
                }
                else
                {
                    Debug.LogWarning("statsMenuController is not assigned!");
                }
            }
        }
        else
        {
            Debug.LogWarning("Menu GameObject is not assigned!");
        }
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
        if (GameObject.FindWithTag("Player") != null)
        {
            GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = false;
        }
        InactiveMenu.SetActive(true);
        Menu.SetActive(false);
        

        
    }
}
