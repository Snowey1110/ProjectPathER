using Unity.Netcode;
using UnityEngine;

public class ArcherAttack : BaseAttack
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;

    // Distance beyond the shooter collider edge.
    [SerializeField] private float extraSpawnPadding = 0.20f;

    public override void Fire(Vector2 direction)
    {
        if (!IsOwner) return;

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;

        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        // Server will compute spawn position from the authoritative shooter transform.
        SpawnArrowServerRpc(direction, NetworkObjectId);
    }

    // RequireOwnership=false avoids intermittent ownership timing rejects.
    [ServerRpc(RequireOwnership = false)]
    private void SpawnArrowServerRpc(Vector2 dir, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        if (arrowPrefab == null)
        {
            Debug.LogError("[ArcherAttack] Server: arrowPrefab is null.");
            return;
        }

        // Find shooter on server
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out var shooterNo) ||
            shooterNo == null)
        {
            Debug.LogWarning($"[ArcherAttack] Server: shooter not found for id={shooterId}");
            return;
        }


        Vector3 shooterPos = shooterNo.transform.position;

        // Compute spawn distance based on shooter collider bounds
        float radius = 0f;
        Collider2D shooterCol = shooterNo.GetComponentInChildren<Collider2D>();
        if (shooterCol != null)
        {
            // A conservative estimate that works for most 2D colliders
            radius = shooterCol.bounds.extents.magnitude;
        }

        Vector3 spawnPos = shooterPos + (Vector3)dir * (radius + extraSpawnPadding);

        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        NetworkObject arrowNo = arrowGo.GetComponent<NetworkObject>();
        if (arrowNo == null)
        {
            Debug.LogError("[ArcherAttack] Arrow prefab missing NetworkObject.");
            Destroy(arrowGo);
            return;
        }

        // Compute damage from shooter stats (server authoritative)
        int dmg = 1;
        stats shooterStats = shooterNo.GetComponent<stats>();
        if (shooterStats != null) dmg = shooterStats.baseDamage;

        arrowNo.Spawn(true);

        Arrow arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.ServerInit(dir, arrowSpeed, dmg, shooterId);
        }

        Debug.Log($"[ArcherAttack] Server spawned arrow. sender={rpcParams.Receive.SenderClientId}, shooterOwner={shooterNo.OwnerClientId}");
    }
}
