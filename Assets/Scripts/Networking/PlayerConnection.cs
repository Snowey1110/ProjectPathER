using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerConnection : NetworkBehaviour
{
    [SerializeField] private Camera lobbyCamera;
    private bool _requestedSpawn;

    private void Start()
    {
        if (!IsOwner && lobbyCamera != null)
            lobbyCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner || _requestedSpawn) return;
        if (lobbyCamera == null || !lobbyCamera.gameObject.activeInHierarchy) return;

        var mouse = Mouse.current;
        if (mouse == null) return; // no mouse device present

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();

            // ScreenToWorldPoint expects Vector3; Z is ignored for orthographic cameras
            Vector3 world = lobbyCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            world.z = 0f;

            RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero);
            if (hit.collider == null) return;

            if (!hit.collider.TryGetComponent(out ClassAltar altar)) return;

            // IMPORTANT: require NetworkObject so server can locate it
            NetworkObject altarNo = altar.GetComponent<NetworkObject>();
            if (altarNo == null)
            {
                Debug.LogError("[PlayerConnection] ClassAltar is missing NetworkObject.");
                return;
            }

            Debug.Log($"Found Altar! Requesting: {altar.classType}");

            _requestedSpawn = true;
            TryUseAltarServerRpc(altarNo.NetworkObjectId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void TryUseAltarServerRpc(ulong altarNetworkObjectId, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        bool success = false;

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(altarNetworkObjectId, out var altarNo))
        {
            var altar = altarNo.GetComponent<ClassAltar>();
            if (altar != null)
            {
                success = altar.ServerTryUse(senderId);
            }
        }

        // Respond only to the requesting client
        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
        };

        OnUseAltarResultClientRpc(success, sendParams);
    }

    [ClientRpc]
    private void OnUseAltarResultClientRpc(bool success, ClientRpcParams rpcParams = default)
    {
        if (!success)
        {
            // Allow clicking other altars
            _requestedSpawn = false;
            return;
        }

        // Success: lobby camera will typically be replaced by the spawned character�s camera.
        if (lobbyCamera != null)
            lobbyCamera.gameObject.SetActive(false);
    }
}
