using Unity.Netcode;
using UnityEngine;

public class ArcherAttack : BaseAttack
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    [SerializeField] private float extraSpawnPadding = 0.20f;

    public override void Fire(Vector2 direction)
    {
        if (!IsOwner) return;

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;

        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        SpawnArrowServerRpc(direction, NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnArrowServerRpc(Vector2 dir, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        if (arrowPrefab == null) return;

        // Find shooter object on server
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out var shooterNo) ||
            shooterNo == null)
        {
            return;
        }

        // Minimal ownership validation (keeps stability but still prevents other clients from spoofing shots)
        if (shooterNo.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        Vector3 shooterPos = shooterNo.transform.position;

        // Spawn outside shooter collider
        float radius = 0f;
        Collider2D shooterCol = shooterNo.GetComponentInChildren<Collider2D>();
        if (shooterCol != null) radius = shooterCol.bounds.extents.magnitude;

        Vector3 spawnPos = shooterPos + (Vector3)dir * (radius + extraSpawnPadding);

        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        var arrowNo = arrowGo.GetComponent<NetworkObject>();
        if (arrowNo == null)
        {
            Destroy(arrowGo);
            return;
        }

        // Damage from shooter stats
        int dmg = 1;
        var shooterStats = shooterNo.GetComponent<stats>();
        if (shooterStats != null) dmg = shooterStats.baseDamage;

        // Read friendly fire + class from PlayerController (server authoritative)
        bool ff = false;
        var shooterPC = shooterNo.GetComponent<PlayerController>();
        if (shooterPC != null) ff = shooterPC.FriendlyFire.Value;

        arrowNo.Spawn(true);

        var arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
            arrow.ServerInit(dir, arrowSpeed, dmg, shooterId, ff);
    }
}
