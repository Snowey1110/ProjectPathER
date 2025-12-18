using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Class Prefabs")]
    [SerializeField] private NetworkObject archerPrefab;
    [SerializeField] private NetworkObject knightPrefab;
    [SerializeField] private NetworkObject magePrefab;
    [SerializeField] private NetworkObject healerPrefab;

    [Header("Optional Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<ClassType, bool> classTakenStatus = new Dictionary<ClassType, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize all classes as free.
        foreach (ClassType t in Enum.GetValues(typeof(ClassType)))
            classTakenStatus[t] = false;
    }

    public bool IsClassAvailable(ClassType type)
    {
        // Server truth.
        if (!classTakenStatus.TryGetValue(type, out bool taken)) return true;
        return !taken;
    }

    public void SpawnCharacter(ulong clientId, ClassType type)
    {
        if (!IsServer)
        {
            Debug.LogError("[LobbyManager] SpawnCharacter called on client. This must run on server.");
            return;
        }

        if (!IsClassAvailable(type))
        {
            Debug.Log("[LobbyManager] Class already taken.");
            return;
        }

        NetworkObject prefab = GetPrefab(type);
        if (prefab == null)
        {
            Debug.LogError("[LobbyManager] Missing prefab for " + type);
            return;
        }

        Vector3 pos = GetSpawnPosition();
        NetworkObject character = Instantiate(prefab, pos, Quaternion.identity);

        // Give ownership to selecting client.
        character.SpawnWithOwnership(clientId, true);

        classTakenStatus[type] = true;
    }

    public void ReleaseClass(ClassType type)
    {
        if (!IsServer) return;
        classTakenStatus[type] = false;
    }

    private NetworkObject GetPrefab(ClassType type)
    {
        return type switch
        {
            ClassType.Archer => archerPrefab,
            ClassType.Knight => knightPrefab,
            ClassType.Mage => magePrefab,
            ClassType.Healer => healerPrefab,
            _ => null
        };
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform t = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            if (t != null) return t.position;
        }

        // Fallback if no spawn points provided
        return Vector3.zero;
    }
}
