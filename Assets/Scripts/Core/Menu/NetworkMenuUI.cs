using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // For IP Address
using TMPro; // For Input Field
using UnityEngine.SceneManagement;

public class NetworkMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField ipAddressInput;

    public void OnStartHostClicked()
    {
        // Host always listens on localhost (0.0.0.0)
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    public void OnJoinGameClicked()
    {
        // Setup the Connection Data
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        string ipText = ipAddressInput.text;
        if (!string.IsNullOrEmpty(ipText))
        {
            transport.ConnectionData.Address = ipText;
        }
        else
        {
            transport.ConnectionData.Address = "127.0.0.1"; // Default to localhost
        }

        // Connect
        NetworkManager.Singleton.StartClient();
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }
}