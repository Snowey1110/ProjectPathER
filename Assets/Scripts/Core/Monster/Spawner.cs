using Unity.Netcode;
using UnityEngine;

public class SlimeSpawner : NetworkBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private float spawnRadius = 30f;
    [SerializeField] private float minimumSpawnDistance = 20f;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int spawnCount = 3;

    [Header("Day / Night")]
    [Tooltip("If true, spawner is paused. If false (night), spawner runs.")]
    [SerializeField] private bool startAsDayTime = true;

    [Tooltip("If true, when switching to night, the spawner timer resets so spawning can happen immediately.")]
    [SerializeField] private bool spawnImmediatelyOnNight = true;

    [Tooltip("Host-only editor test hotkey to toggle day/night.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F6;

    // Server authoritative replicated flag
    private NetworkVariable<bool> isDayTime = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float spawnTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Clients do not run spawning logic.
            enabled = false;
            return;
        }

        isDayTime.Value = startAsDayTime;
        spawnTimer = spawnInterval;

        isDayTime.OnValueChanged += OnDayTimeChanged;
    }

    private void OnDestroy()
    {
        if (IsServer)
            isDayTime.OnValueChanged -= OnDayTimeChanged;
    }

    private void OnDayTimeChanged(bool oldValue, bool newValue)
    {
        // Transition: day -> night
        if (oldValue == true && newValue == false && spawnImmediatelyOnNight)
        {
            spawnTimer = 0f;
        }

    }

    private void Update()
    {
        if (!IsServer) return;

        // Host-only editor testing: toggle day/night
        if (IsHost && Input.GetKeyDown(toggleKey))
        {
            SetDayTime(!isDayTime.Value);
            Debug.Log($"[SlimeSpawner] Toggled DayTime -> {isDayTime.Value}");
        }

        // Only spawn at night
        if (isDayTime.Value) return;

        if (slimePrefab == null) return;

        Transform targetPlayer = GetAnyPlayerTransform();
        if (targetPlayer == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnSlimes(targetPlayer);
        spawnTimer = spawnInterval;
    }

    /// Server-side setter for day/night.
    /// Call this from other server systems if needed.
    public void SetDayTime(bool day)
    {
        if (!IsServer) return;
        isDayTime.Value = day;
    }

    public bool IsDayTime()
    {
        return isDayTime.Value;
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
