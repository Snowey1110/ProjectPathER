using UnityEngine;
using UnityEngine.InputSystem;

public class OptionsMenuController : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject optionsPanel; // Drag Options Panel here

    void Start()
    {
        // Ensure it starts hidden
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    void Update()
    {
        // Listen for ESC key
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Only close if it is currently open
            if (optionsPanel.activeSelf)
            {
                CloseOptions();
            }
        }
    }

    // Call this from your "Options" Button
    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    // Call this from your "Back" Button (optional)
    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }
}