using UnityEngine;
using Unity.Netcode;
using Netcode.Transports.Facepunch; // Swapped UTP for Facepunch
using Steamworks; // Added Steamworks
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;

public class NetworkMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField steamIdInputField; // Renamed for clarity
    public GameObject uiVisuals; // Drag your entire Menu Canvas/Panel here

    // Singleton check to prevent duplicates if you return to menu
    private static NetworkMenuUI instance;

    [Header("Status text")]
    [Tooltip("If assigned, status messages will be shown here in addition to Debug.Log.")]
    public TMP_Text statusText;

    private FacepunchTransport facepunchTransport;
    private UnityTransport unityTransport;

    private bool SteamAvailable => SteamClient.IsValid;

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
        CacheTransports();

        // Steam is OPTIONAL. We only require it for Steam multiplayer.
        // If Steam isn't available, the game can still run in "singleplayer" using UnityTransport.
        TryInitSteamClient();

        // Default behavior:
        // - If Steam is available -> keep Steam transport enabled.
        // - If not -> switch to UnityTransport so Singleplayer works immediately.
        if (!SteamAvailable)
        {
            SelectUnityTransport();
            SetStatus("Steam not available. Singleplayer (local) is enabled; multiplayer requires Steam.");
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
        // Run Steam callbacks only when Steam is actually initialized.
        if (SteamAvailable)
        {
            SteamClient.RunCallbacks();
        }
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
        // Shutdown Steam SECOND (only if it was initialized)
        try
        {
            if (SteamAvailable) SteamClient.Shutdown();
        }
        catch { /* ignore shutdown exceptions */ }
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
        if (!SteamAvailable)
        {
            SetStatus("Steam is not available. Multiplayer requires Steam (launch through Steam and ensure you are logged in).");
            return;
        }

        SelectFacepunchTransport();

        // Logic: Parse Steam ID instead of IP
        string idText = steamIdInputField.text;

        if (ulong.TryParse(idText, out ulong friendId))
        {
            Debug.Log($"Connecting to Steam ID: {friendId}...");

            // Set the Facepunch Transport Target
            facepunchTransport.targetSteamId = friendId;

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
        // If Steam isn't available, fall back to local singleplayer.
        if (!SteamAvailable)
        {
            SetStatus("Steam is not available. Starting Singleplayer (local host) instead.");
            StartSingleplayer();
            return;
        }

        Debug.Log("Starting Host via Steam...");
        SelectFacepunchTransport();

        // Facepunch handles the "Target ID" automatically for hosts (it uses your own)
        facepunchTransport.targetSteamId = SteamClient.SteamId;

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    // Hook this to a "Singleplayer" button (recommended).
    public void OnSingleplayerClicked()
    {
        StartSingleplayer();
    }

    private void StartSingleplayer()
    {
        SelectUnityTransport();
        Debug.Log("Starting Singleplayer (local host via UnityTransport)...");
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    private void CacheTransports()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null. Make sure the NetworkManager exists in the MainMenu scene.");
            return;
        }

        facepunchTransport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
        unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // If UnityTransport isn't present in the scene, add it at runtime so singleplayer can still work.
        if (unityTransport == null)
        {
            unityTransport = NetworkManager.Singleton.gameObject.AddComponent<UnityTransport>();
        }

        if (facepunchTransport == null)
        {
            Debug.LogWarning("FacepunchTransport is missing on NetworkManager. Steam multiplayer will not work.");
        }
    }

    private void TryInitSteamClient()
    {
        // Initialize Steam Client if possible. If this fails, we still allow singleplayer.
        if (SteamClient.IsValid) return;

        try
        {
            SteamClient.Init(480);
            Debug.Log($"Steam Initialized: {SteamClient.Name}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Steam not initialized (singleplayer is still available): " + e.Message);
        }
    }

    private void SelectFacepunchTransport()
    {
        CacheTransports();

        if (facepunchTransport == null)
        {
            SetStatus("FacepunchTransport missing; cannot start Steam multiplayer.");
            return;
        }

        if (unityTransport != null) unityTransport.enabled = false;
        facepunchTransport.enabled = true;

        NetworkManager.Singleton.NetworkConfig.NetworkTransport = facepunchTransport;
    }

    private void SelectUnityTransport()
    {
        CacheTransports();

        if (unityTransport == null)
        {
            SetStatus("UnityTransport missing; cannot start singleplayer.");
            return;
        }

        if (facepunchTransport != null) facepunchTransport.enabled = false;
        unityTransport.enabled = true;

        // Local host defaults
        unityTransport.ConnectionData.Address = "127.0.0.1";
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
    }

    private void SetStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null)
        {
            statusText.text = message;
        }
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