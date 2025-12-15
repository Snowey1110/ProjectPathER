using UnityEngine;
using Unity.Netcode;

public class ArcherAttack : BaseAttack
{
    [Header("Settings")]
    public GameObject arrowPrefab;
    public float arrowForce = 20f;

    public float spawnOffset = 1.5f; // Distance from center to spawn arrow

    public override void Fire(Vector2 direction)
    {
        // Cooldown Check
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;

        // CALCULATE SPAWN POSITION WITH MATH
        // Start at my center + (Direction * Distance)
        Vector3 spawnPos = transform.position + (Vector3)(direction * spawnOffset);

        // Tell Server to spawn
        SpawnArrowServerRpc(direction, spawnPos);
    }

    [ServerRpc]
    private void SpawnArrowServerRpc(Vector2 dir, Vector3 spawnPos)
    {
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        arrow.GetComponent<NetworkObject>().Spawn();

        // Rotate arrow to face direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Add Force
        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * arrowForce;
    }
}