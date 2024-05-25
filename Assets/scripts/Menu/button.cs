using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class button : MonoBehaviour
{
    // Start is called before the first frame update
    
    public void SwitchToGameScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void SwitchToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void statsPage()
    {
        GameObject statsPage = GameObject.FindGameObjectWithTag("LevelController");
        MenuToggle statsMenu = statsPage.GetComponent<MenuToggle>();
        statsMenu.toggleMenu();
    }
    public void QutiGame()
    {
        Application.Quit();
    }
    


}
