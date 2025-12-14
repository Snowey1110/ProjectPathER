using UnityEngine;
using Unity.Netcode;

public class PlayerConnection : NetworkBehaviour
{
    [SerializeField] private Camera lobbyCamera;

    public override void OnNetworkSpawn()
    {
        // Only the owner (you) should use this camera.
        // Disable it for other players so you don't see through their eyes.
        if (!IsOwner)
        {
            lobbyCamera.gameObject.SetActive(false);
            GetComponent<AudioListener>().enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = lobbyCamera.ScreenPointToRay(Input.mousePosition);

            // Draw a visible red line in the Scene View to see where you aimed
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Tell me EXACTLY what I hit
                Debug.Log($"I hit: {hit.transform.name}");

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