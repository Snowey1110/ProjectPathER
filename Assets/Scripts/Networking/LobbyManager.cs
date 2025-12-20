using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Player Prefabs")]
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject healerPrefab;

    [Header("Spawn")]
    [SerializeField] private Vector3 baseSpawn = new Vector3(0, 0, 0);
    [SerializeField] private float spawnSeparation = 4f;

    [Header("Rules")]
    [SerializeField] private bool enforceUniqueClasses = false;

    private readonly Dictionary<ClassType, bool> classTakenStatus = new();
    private readonly Dictionary<ulong, NetworkObject> spawnedCharacters = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (ClassType type in System.Enum.GetValues(typeof(ClassType)))
            classTakenStatus[type] = false;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (spawnedCharacters.TryGetValue(clientId, out NetworkObject no) && no != null && no.IsSpawned)
            no.Despawn(true);

        spawnedCharacters.Remove(clientId);

        // If you enforce unique classes, you’d also want to release the class here
        // (requires tracking which class each client picked).
    }

    public bool TrySpawnCharacter(ulong clientId, ClassType classType)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[LobbyManager] TrySpawnCharacter called on non-server.");
            return false;
        }

        if (spawnedCharacters.ContainsKey(clientId))
        {
            Debug.LogWarning($"[LobbyManager] Client {clientId} already has a character.");
            return false;
        }

        if (enforceUniqueClasses && classTakenStatus.TryGetValue(classType, out bool taken) && taken)
        {
            Debug.LogWarning($"[LobbyManager] Class {classType} already taken.");
            return false;
        }

        GameObject prefab = GetPrefab(classType);
        if (prefab == null)
        {
            Debug.LogError($"[LobbyManager] Missing prefab for class {classType}. Check inspector assignments.");
            return false;
        }

        // Spread spawns out by clientId so they don't overlap
        int slot = (int)(clientId % 8);
        Vector3 spawnPos = baseSpawn + new Vector3(slot * spawnSeparation, 0f, 0f);

        GameObject playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError($"[LobbyManager] Prefab {prefab.name} has no NetworkObject.");
            Destroy(playerObj);
            return false;
        }

        // Give ownership to the requesting client
        netObj.SpawnWithOwnership(clientId);

        spawnedCharacters[clientId] = netObj;

        if (enforceUniqueClasses)
            classTakenStatus[classType] = true;

        return true;
    }

    private GameObject GetPrefab(ClassType classType)
    {
        return classType switch
        {
            ClassType.Archer => archerPrefab,
            ClassType.Knight => knightPrefab,
            ClassType.Mage => magePrefab,
            ClassType.Healer => healerPrefab,
            _ => null
        };
    }
}
