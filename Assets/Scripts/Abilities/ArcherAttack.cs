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
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;

        Vector3 spawnPos = transform.position + (Vector3)(direction * spawnOffset);
        SpawnArrowServerRpc(direction, spawnPos);
    }

    [ServerRpc]
    private void SpawnArrowServerRpc(Vector2 dir, Vector3 spawnPos)
    {
        if (arrowPrefab == null) return;

        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        var no = arrowGo.GetComponent<NetworkObject>();
        if (no == null)
        {
            Debug.LogError("[ArcherAttack] Arrow prefab missing NetworkObject.");
            Destroy(arrowGo);
            return;
        }

        // Rotate to face direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowGo.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Compute damage from shooter stats on server
        int dmg = 1;
        var shooterStats = GetComponent<stats>();
        if (shooterStats != null) dmg = shooterStats.baseDamage;

        // Spawn network object then init on server
        no.Spawn(true);

        var arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
            arrow.ServerInit(dir, arrowSpeed, dmg);
    }
}
