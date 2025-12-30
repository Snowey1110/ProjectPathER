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

        SpawnArrowRpc(direction, NetworkObjectId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnArrowRpc(Vector2 dir, ulong shooterId, RpcParams rpcParams = default)
    {
        if (arrowPrefab == null) return;

        // Find shooter object on server
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out var shooterNo) ||
            shooterNo == null)
        {
            return;
        }

        // Validate ownership: only the owner of shooter can request shots
        if (shooterNo.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Vector3 shooterPos = shooterNo.transform.position;

        // Spawn outside shooter collider
        float radius = 0f;
        Collider2D shooterCol = shooterNo.GetComponentInChildren<Collider2D>();
        if (shooterCol != null) radius = shooterCol.bounds.extents.magnitude;

        Vector3 spawnPos = shooterPos + (Vector3)dir * (radius + extraSpawnPadding);

        // Compute rotation BEFORE Spawn() so clients receive correct orientation in spawn payload
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Read offset from prefab's Arrow component (so art alignment stays in one place)
        float offsetDeg = 0f;
        var prefabArrow = arrowPrefab.GetComponent<Arrow>();
        if (prefabArrow != null)
            offsetDeg = prefabArrow.SpriteAngleOffsetDeg;

        Quaternion spawnRot = Quaternion.Euler(0f, 0f, angle + offsetDeg);

        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, spawnRot);

        var arrowNo = arrowGo.GetComponent<NetworkObject>();
        if (arrowNo == null)
        {
            Destroy(arrowGo);
            return;
        }

        // Damage from shooter stats
        int dmg = 1;
        var shooterStats = shooterNo.GetComponent<stats>();
        if (shooterStats != null) dmg = shooterStats.Damage.Value;

        // Friendly fire from PlayerController (server authoritative)
        bool ff = false;
        var shooterPC = shooterNo.GetComponent<PlayerController>();
        if (shooterPC != null) ff = shooterPC.FriendlyFire.Value;

        // Spawn on network (clients will create with spawnRot)
        arrowNo.Spawn(true);

        // Initialize server physics + collision ignore rules
        var arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
            arrow.ServerInit(dir, arrowSpeed, dmg, shooterId, ff);
    }
}
