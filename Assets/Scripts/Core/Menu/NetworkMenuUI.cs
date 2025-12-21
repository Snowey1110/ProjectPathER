using UnityEngine;
using Unity.Netcode;
using Netcode.Transports.Facepunch; // Swapped UTP for Facepunch
using Steamworks; // Added Steamworks
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField steamIdInputField; // Renamed for clarity
    public GameObject uiVisuals; // Drag your entire Menu Canvas/Panel here

    // Singleton check to prevent duplicates if you return to menu
    private static NetworkMenuUI instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keeps Steam alive during the game
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Initialize Steam Client
        if (!SteamClient.IsValid)
        {
            try
            {
                SteamClient.Init(480);
                Debug.Log($"Steam Initialized: {SteamClient.Name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Steam Failed to Init: " + e.Message);
            }
        }

        // Hide the input field initially
        if (steamIdInputField != null)
            steamIdInputField.gameObject.SetActive(false);

        // Listen for "Enter" key
        if (steamIdInputField != null)
            steamIdInputField.onSubmit.AddListener(OnInputSubmit);

        // Listen for Scene Changes so we can hide/show the menu
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        // Run Steam Callbacks every frame
        SteamClient.RunCallbacks();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        // This runs only when the game executable actually closes.

        // Shutdown Netcode FIRST
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Shutdown Steam SECOND
        // This gives the Transport time to clean up its sockets before we kill the Steam Client.
        SteamClient.Shutdown();
    }


    // --- BUTTON LOGIC ---

    public void OnMultiplayerButtonClicked()
    {
        if (steamIdInputField.gameObject.activeSelf)
        {
            AttemptConnection();
        }
        else
        {
            steamIdInputField.gameObject.SetActive(true);
            steamIdInputField.Select();
            steamIdInputField.ActivateInputField();
        }
    }

    private void OnInputSubmit(string text)
    {
        AttemptConnection();
    }

    private void AttemptConnection()
    {
        // Logic: Parse Steam ID instead of IP
        string idText = steamIdInputField.text;

        if (ulong.TryParse(idText, out ulong friendId))
        {
            Debug.Log($"Connecting to Steam ID: {friendId}...");

            // Set the Facepunch Transport Target
            var transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
            transport.targetSteamId = friendId;

            // Start Client
            NetworkManager.Singleton.StartClient();

            // Hide Input
            steamIdInputField.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Invalid Steam ID! Copy it from your friend's console.");
        }
    }

    public void OnStartHostClicked()
    {
        Debug.Log("Starting Host via Steam...");

        // Facepunch handles the "Target ID" automatically for hosts (it uses your own)
        var transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
        transport.targetSteamId = SteamClient.SteamId;

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }

    // --- HELPER TO HIDE UI IN GAME ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (uiVisuals == null) return;

        if (scene.name == "SampleScene") // Change this to your Game Scene name
        {
            uiVisuals.SetActive(false); // Hide Menu when playing
        }
        else
        {
            uiVisuals.SetActive(true); // Show Menu when in menu scene
            // Unlock mouse cursor when returning to menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}