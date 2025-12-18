using UnityEngine;
using Unity.Netcode;

public class PlayerConnection : NetworkBehaviour
{
    [SerializeField] private Camera lobbyCamera;

    private AudioListener lobbyListener;

    public override void OnNetworkSpawn()
    {
        lobbyListener = lobbyCamera != null ? lobbyCamera.GetComponent<AudioListener>() : null;

        // Ensure only the local owner has an active camera/audio under this NetworkObject.
        SetLobbyCameraLocalState(IsOwner);
    }

    private void SetLobbyCameraLocalState(bool isLocalOwner)
    {
        if (lobbyCamera == null) return;

        // Keep the GameObject active (avoid weirdness with disabling parts of a networked hierarchy)
        if (!lobbyCamera.gameObject.activeSelf)
            lobbyCamera.gameObject.SetActive(true);

        lobbyCamera.enabled = isLocalOwner;

        if (lobbyListener != null)
            lobbyListener.enabled = isLocalOwner;

        // Prevent Camera.main from resolving to another client's camera
        lobbyCamera.tag = isLocalOwner ? "MainCamera" : "Untagged";
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (lobbyCamera == null || !lobbyCamera.enabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = lobbyCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider == null)
            {
                Debug.Log("I clicked, but the Raycast missed everything.");
                return;
            }

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
    }

    [ServerRpc]
    private void RequestSpawnServerRpc(ClassType classType, ServerRpcParams rpcParams = default)
    {
        if (LobbyManager.Instance == null) return;

        // Use the actual sender, not OwnerClientId (more robust in host / ownership edge cases).
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!LobbyManager.Instance.IsClassAvailable(classType))
        {
            Debug.Log("Class is already taken!");
            return;
        }

        LobbyManager.Instance.SpawnCharacter(senderClientId, classType);

        // Disable lobby cam ONLY for the player who clicked the altar.
        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { senderClientId }
            }
        };
        DisableLobbyCameraClientRpc(sendParams);
    }

    [ClientRpc]
    private void DisableLobbyCameraClientRpc(ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        if (lobbyCamera == null) return;

        lobbyCamera.enabled = false;
        if (lobbyListener != null) lobbyListener.enabled = false;

        // Also untag so Camera.main won't point here anymore
        lobbyCamera.tag = "Untagged";
    }
}
