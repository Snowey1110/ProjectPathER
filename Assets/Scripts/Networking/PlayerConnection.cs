using UnityEngine;
using Unity.Netcode;

public class PlayerConnection : NetworkBehaviour
{
    [SerializeField] private Camera lobbyCamera;

    private bool _requestedSpawn;

    private void Start()
    {
        // Each client should only see their own lobby camera
        if (!IsOwner && lobbyCamera != null)
            lobbyCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner || _requestedSpawn) return;
        if (lobbyCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = lobbyCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero);

            if (hit.collider == null) return;

            ClassAltar altar = hit.collider.GetComponent<ClassAltar>();
            if (altar == null) return;

            Debug.Log($"Found Altar! Requesting: {altar.classType}");
            _requestedSpawn = true;
            RequestSpawnServerRpc(altar.classType);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestSpawnServerRpc(ClassType classType, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        bool success = LobbyManager.Instance != null && LobbyManager.Instance.TrySpawnCharacter(senderId, classType);

        if (!success)
        {
            // Allow the client to try again if rejected
            ReenableRequestClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
            });
            return;
        }

        // Disable lobby camera ONLY for the requesting client
        DisableLobbyCameraClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
        });
    }

    [ClientRpc]
    private void DisableLobbyCameraClientRpc(ClientRpcParams rpcParams = default)
    {
        if (lobbyCamera != null)
            lobbyCamera.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void ReenableRequestClientRpc(ClientRpcParams rpcParams = default)
    {
        _requestedSpawn = false;
    }
}
