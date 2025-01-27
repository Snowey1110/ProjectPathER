using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;
    public GameObject escapeMenu;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                UnpauseGame();
                escapeMenu.SetActive(false);
            }
            else {
                if (SceneManager.GetActiveScene().name == "SampleScene")
                { 
                    PauseGame();
                    escapeMenu.SetActive(true);
                }
            }
            
        }
    }

    public void PauseGame()
    {
        
        Time.timeScale = 1f;
        isPaused = true;
        if (GameObject.FindWithTag("Player") != null)
        {
            GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = false;
        }
    }

    public void UnpauseGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (escapeMenu != null)
        {
            escapeMenu.SetActive(false);
        }
        
        if (GameObject.FindWithTag("Player") != null)
        {
            GameObject.FindWithTag("Player").GetComponent<stats>().allowCombat = true;
        }
        
    }

    public void MainMenuButton()
    {
        UnpauseGame();
        SceneManager.LoadScene("MainMenu");
    }
}