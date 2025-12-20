using Unity.Netcode;
using UnityEngine;

public class SlimeSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private float spawnRadius = 30f;
    [SerializeField] private float minimumSpawnDistance = 20f;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int spawnCount = 3;

    private float spawnTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (slimePrefab == null) return;

        Transform targetPlayer = GetAnyPlayerTransform();
        if (targetPlayer == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnSlimes(targetPlayer);
        spawnTimer = spawnInterval;
    }

    private Transform GetAnyPlayerTransform()
    {
        if (NetworkManager.Singleton == null) return null;

        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (c?.PlayerObject != null)
                return c.PlayerObject.transform;
        }

        return null;
    }

    private void SpawnSlimes(Transform player)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition;
            int guard = 0;

            do
            {
                Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
                randomOffset.z = 0f;
                spawnPosition = player.position + randomOffset;
                guard++;
            }
            while (Vector3.Distance(spawnPosition, player.position) < minimumSpawnDistance && guard < 50);

            GameObject slime = Instantiate(slimePrefab, spawnPosition, Quaternion.identity);

            var no = slime.GetComponent<NetworkObject>();
            if (no == null)
            {
                Debug.LogError("[SlimeSpawner] Slime prefab missing NetworkObject.");
                Destroy(slime);
                continue;
            }

            no.Spawn(true);
        }
    }
}
