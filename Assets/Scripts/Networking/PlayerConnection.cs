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

    private void Update()
    {
        if (!IsOwner || _requestedSpawn) return;
        if (lobbyCamera == null || !lobbyCamera.gameObject.activeInHierarchy) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = lobbyCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero);

            if (hit.collider == null) return;

            // Prefer TryGetComponent for perf/clarity
            if (!hit.collider.TryGetComponent(out ClassAltar altar)) return;

            Debug.Log($"Found Altar! Requesting: {altar.classType}");

            _requestedSpawn = true;

            // Altar owns the "claimed" rule and calls LobbyManager on the server.
            altar.TryUseServerRpc();

        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        // If you want: allow manual retry if nothing happened for some reason.
        // Press R to unlock request state.
        if (_requestedSpawn && Input.GetKeyDown(KeyCode.R))
            _requestedSpawn = false;
    }
}
