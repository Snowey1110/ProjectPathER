using UnityEngine;

public class SlimeSpawner : MonoBehaviour
{
    public GameObject slimePrefab;
    public float spawnRadius = 30f; // Maximum radius around the player where slimes spawn
    public float minimumSpawnDistance = 20f; // Minimum distance from the player for spawning slimes
    public float spawnInterval = 10f; // Time in seconds between spawns
    public int spawnCount = 3; // Number of slimes to spawn each time
    public string playerTag = "Player";

    private Transform player; // Reference to the player's transform
    private float spawnTimer;

    private void Start()
    {
        spawnTimer = spawnInterval; // Initialize the timer
    }

    private void Update()
    {
        if (player == null)
        {
            // Try to find the player in the scene
            FindPlayer();
            if (player == null)
            {
                // If the player is still not found, skip this frame
                return;
            }
        }

        // Countdown the spawn timer
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnSlimes();
            spawnTimer = spawnInterval; // Reset the timer
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("Player found and assigned to spawner.");
        }
    }

    private void SpawnSlimes()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition;

            // Ensure slimes spawn within the radius but outside the minimum distance
            do
            {
                Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
                spawnPosition = player.position + randomOffset;
            } 
            while (Vector3.Distance(spawnPosition, player.position) < minimumSpawnDistance);

            // Spawn the slime prefab
            Instantiate(slimePrefab, spawnPosition, Quaternion.identity);
        }
    }
}
