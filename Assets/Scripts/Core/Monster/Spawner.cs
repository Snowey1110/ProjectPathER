using Unity.Netcode;
using UnityEngine;

public class SlimeSpawner : NetworkBehaviour
{
    [Header("Prefab (MUST have NetworkObject + NetworkTransform or NetworkRigidbody2D)")]
    [SerializeField] private NetworkObject slimePrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 30f;
    [SerializeField] private float minimumSpawnDistance = 20f;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private string playerTag = "Player";

    private float spawnTimer;

    public override void OnNetworkSpawn()
    {
        // Only the server spawns monsters.
        spawnTimer = spawnInterval;
        enabled = IsServer;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (slimePrefab == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = spawnInterval;
        SpawnSlimesServer();
    }

    private void SpawnSlimesServer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        if (players == null || players.Length == 0) return;

        // Pick a random player to spawn around (server-side).
        Transform targetPlayer = players[Random.Range(0, players.Length)].transform;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition;
            int guard = 0;

            // Ensure we’re outside minimum distance.
            do
            {
                Vector3 randomOffset = (Vector3)Random.insideUnitCircle * spawnRadius;
                spawnPosition = targetPlayer.position + randomOffset;
                guard++;
            }
            while (Vector3.Distance(spawnPosition, targetPlayer.position) < minimumSpawnDistance && guard < 50);

            NetworkObject slime = Instantiate(slimePrefab, spawnPosition, Quaternion.identity);
            slime.Spawn(true);
        }
    }
}
