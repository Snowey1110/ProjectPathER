using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance; // Singleton so any script can access it

    [Header("Menus")]
    public GameObject pauseMenu;
    public GameObject statsMenu;
    public GameObject classSelectMenu; // The menu with the 4 buttons

    private bool isMenuOpen = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // Toggle Pause Menu with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }

        // Toggle Stats Menu with 'I' or 'Tab' (Example)
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleStatsMenu();
        }
    }

    public void TogglePauseMenu()
    {
        bool isActive = !pauseMenu.activeSelf;

        // Close others first
        CloseAllMenus();

        pauseMenu.SetActive(isActive);
        HandleInputLock(isActive);
    }

    public void ToggleStatsMenu()
    {
        bool isActive = !statsMenu.activeSelf;

        CloseAllMenus();

        statsMenu.SetActive(isActive);
        HandleInputLock(isActive);
    }

    public void CloseAllMenus()
    {
        pauseMenu.SetActive(false);
        statsMenu.SetActive(false);
        // Do NOT close ClassSelect here, that's special logic
        HandleInputLock(false);
    }

    // MULTIPLAYER PAUSE: We don't stop Time. We stop the Player.
    private void HandleInputLock(bool isLocked)
    {
        isMenuOpen = isLocked;

        // Show/Hide Mouse Cursor
        Cursor.visible = isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;

        // Find OUR Local Player and disable their controls
        if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
        {
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
            if (localPlayer != null)
            {
                localPlayer.SetInputActive(!isLocked); // You need to add this method to PlayerController
            }
        }
    }

    // Called by the "Quit to Main Menu" button
    public void QuitToMainMenu()
    {
        // Shutdown Network properly
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}