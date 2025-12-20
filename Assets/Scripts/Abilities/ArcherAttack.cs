using Unity.Netcode;
using UnityEngine;

public class ArcherAttack : BaseAttack
{
    [Header("Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    [SerializeField] private float spawnOffset = 1.5f;

    public override void Fire(Vector2 direction)
    {
        // Only the owning client should request shots.
        if (!IsOwner) return;

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;

        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        if (arrowPrefab == null)
        {
            Debug.LogError("[ArcherAttack] arrowPrefab is not assigned on this prefab instance.");
            return;
        }

        Vector3 spawnPos = transform.position + (Vector3)direction * spawnOffset;

        // Helpful: tells you the request was sent from the client
        Debug.Log($"[ArcherAttack] Client Fire -> ServerRpc. OwnerClientId={OwnerClientId}, LocalClientId={NetworkManager.Singleton.LocalClientId}");

        SpawnArrowServerRpc(direction, spawnPos, NetworkObjectId);
    }

    // RequireOwnership=false prevents “sometimes rejected” during spawn/ownership timing edge cases.
    // We still validate sender is the owner to prevent cheating.
    [ServerRpc(RequireOwnership = false)]
    private void SpawnArrowServerRpc(Vector2 dir, Vector3 spawnPos, ulong shooterNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        // Validate: sender must own this ArcherAttack’s NetworkObject
        if (sender != OwnerClientId)
        {
            Debug.LogWarning($"[ArcherAttack] Rejected shot: sender {sender} != OwnerClientId {OwnerClientId}");
            return;
        }

        if (arrowPrefab == null)
        {
            Debug.LogError("[ArcherAttack] Server: arrowPrefab is null (inspector assignment missing on prefab?).");
            return;
        }

        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        NetworkObject arrowNo = arrowGo.GetComponent<NetworkObject>();
        if (arrowNo == null)
        {
            Debug.LogError("[ArcherAttack] Arrow prefab missing NetworkObject component.");
            Destroy(arrowGo);
            return;
        }

        // Damage from shooter stats (server authoritative)
        int dmg = 1;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterNetworkObjectId, out var shooterNo))
        {
            var st = shooterNo.GetComponent<stats>();
            if (st != null) dmg = st.baseDamage;
        }

        // Spawn first so clients create the object
        arrowNo.Spawn(true);

        // Initialize projectile
        Arrow arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.ServerInit(dir, arrowSpeed, dmg);
        }
        else
        {
            // fallback if you didn’t put Arrow script on prefab
            Rigidbody2D rb = arrowGo.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * arrowSpeed;
        }

        Debug.Log($"[ArcherAttack] Server spawned arrow for sender={sender}");
    }
}
