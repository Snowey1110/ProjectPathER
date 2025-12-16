using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // For IP Address
using TMPro; // For Input Field
using UnityEngine.SceneManagement;

public class NetworkMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField ipInputField;
    private void Start()
    {
        // Hide the input field when the game starts
        if (ipInputField != null)
            ipInputField.gameObject.SetActive(false);

        // Listen for the "Enter" key specifically on this input field
        // When user hits Enter, we pass the text to our connection logic
        if (ipInputField != null)
            ipInputField.onSubmit.AddListener(OnInputSubmit);
    }

    // Link this to your "Multiplayer/Join" Button
    public void OnMultiplayerButtonClicked()
    {
        // Check: Is the input field currently visible?
        if (ipInputField.gameObject.activeSelf)
        {
            AttemptConnection();
        }
        else
        {
            // Show it!
            ipInputField.gameObject.SetActive(true);

            ipInputField.Select();
            ipInputField.ActivateInputField();
        }
    }

    // Called automatically when user presses "Enter" while typing
    private void OnInputSubmit(string text)
    {
        AttemptConnection();
    }

    private void AttemptConnection()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        string ipText = ipInputField.text;

        // Logic: If empty -> Use Localhost. If filled -> Use Input.
        if (string.IsNullOrEmpty(ipText))
        {
            transport.ConnectionData.Address = "0.0.0.0";
            Debug.Log("Connecting to Localhost...");
        }
        else
        {
            transport.ConnectionData.Address = ipText;
            Debug.Log($"Connecting to {ipText}...");
        }

        // Connect!
        NetworkManager.Singleton.StartClient();

        // Hide the input again after clicking
        ipInputField.gameObject.SetActive(false);
    }

    // Link this to "Start Host" button
    public void OnStartHostClicked()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }
}