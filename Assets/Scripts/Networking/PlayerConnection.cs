using UnityEngine;
using Unity.Netcode;

public class PlayerConnection : NetworkBehaviour
{
    [SerializeField] private Camera lobbyCamera;

    public override void OnNetworkSpawn()
    {
        // Only the owner (you) should use this camera.
        // Disable it for other players so you don't see through their eyes.
        if (IsOwner)
        {
            // Turn on MY camera and listener
            lobbyCamera.enabled = true;

            // If the listener is on the same object as the camera:
            if (lobbyCamera.TryGetComponent(out AudioListener listener))
            {
                listener.enabled = true;
            }

            // Also ensure the GameObject is active (if its disabled the whole object)
            lobbyCamera.gameObject.SetActive(true);
        }
        else
        {
            // No, this belongs to someone else.
            // Ensure their camera stays OFF so I don't see through their eyes.
            lobbyCamera.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("I hit: " + hit.collider.name);

                if (hit.transform.TryGetComponent(out ClassAltar altar))
                {
                    Debug.Log($"Found Altar! Requesting: {altar.classType}");
                    RequestSpawnServerRpc(altar.classType);
                }
                else
                {
                    Debug.Log("I hit something, but it is NOT an Altar.");
                }
            }
            else
            {
                Debug.Log("I clicked, but the Raycast missed everything.");
            }
        }
    }

    [ServerRpc]
    private void RequestSpawnServerRpc(ClassType classType)
    {
        // Ask the Game Manager: "Is this class available?"
        if (LobbyManager.Instance.IsClassAvailable(classType))
        {
            // Spawn the Character
            LobbyManager.Instance.SpawnCharacter(OwnerClientId, classType);

            // Tell the Client to disable their Lobby Camera (so Game Camera takes over)
            DisableLobbyCameraClientRpc();
        }
        else
        {
            Debug.Log("Class is already taken!");
        }
    }

    [ClientRpc]
    private void DisableLobbyCameraClientRpc()
    {
        lobbyCamera.gameObject.SetActive(false);
    }
}