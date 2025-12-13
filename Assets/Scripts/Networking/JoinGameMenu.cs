using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField ipAddressInput; // IP Input Field in main menu drag to here

    public void StartHostGame()
    {
        // Host always listens on "0.0.0.0" (Everything), so no setup needed here.
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        // Get the Transport Component
        // The UnityTransport script controls the IP and Port.
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Set the IP Address
        // If the player typed something, use it. Otherwise, use localhost.
        string ipText = ipAddressInput.text;

        if (!string.IsNullOrEmpty(ipText))
        {
            transport.ConnectionData.Address = ipText;
        }
        else
        {
            transport.ConnectionData.Address = "127.0.0.1";
        }

        // Connect
        NetworkManager.Singleton.StartClient();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}