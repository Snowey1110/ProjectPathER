using UnityEngine;
using Unity.Netcode;

public class ArcherAttack : BaseAttack
{
    [Header("Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    [SerializeField] private float spawnOffset = 1.5f;

    public override void Fire(Vector2 direction)
    {
        if (!IsOwner) return;

        // Cooldown
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;

        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        Vector3 spawnPos = transform.position + (Vector3)direction * spawnOffset;

        // Pass the shooter’s NetworkObjectId so server can read damage/stats.
        SpawnArrowServerRpc(direction, spawnPos, NetworkObjectId);
    }

    [ServerRpc]
    private void SpawnArrowServerRpc(Vector2 dir, Vector3 spawnPos, ulong shooterId)
    {
        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        NetworkObject no = arrowGo.GetComponent<NetworkObject>();
        if (no == null)
        {
            Debug.LogError("[ArcherAttack] Arrow prefab missing NetworkObject.");
            Destroy(arrowGo);
            return;
        }

        // Rotate to face direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowGo.transform.rotation = Quaternion.Euler(0, 0, angle);

        no.Spawn(true);

        // Initialize server-only physics + damage
        Arrow arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
        {
            int dmg = 1;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out var shooterNo))
            {
                stats shooterStats = shooterNo.GetComponent<stats>();
                if (shooterStats != null) dmg = shooterStats.baseDamage;
            }

            arrow.ServerInit(dir, arrowSpeed, dmg);
        }
        else
        {
            Debug.LogWarning("[ArcherAttack] Arrow prefab missing Arrow script.");
        }
    }
}
