using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class button : MonoBehaviour
{
    // Start is called before the first frame update

    public void StartHostGame()
    {
        // Start the Host (Server + Client)
        NetworkManager.Singleton.StartHost();

        // Tell the Network to load the Game Scene
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        // Clients just connect. The Server will automatically pull them into the correct scene.
        NetworkManager.Singleton.StartClient();
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
