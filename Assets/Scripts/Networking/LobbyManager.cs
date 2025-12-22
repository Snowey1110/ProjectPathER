using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Class Prefabs (must have NetworkObject + be registered as NetworkPrefabs)")]
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject healerPrefab;

    [Header("Spawn Points (optional)")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Rules")]
    [Tooltip("If true: each class can only be spawned once (one altar = one class forever unless freed manually).")]
    [SerializeField] private bool enforceUniqueClasses = true;

    [Tooltip("If true: when a client disconnects, their character despawns and their class becomes available again.")]
    [SerializeField] private bool freeClassOnDisconnect = false;

    // classType -> taken?
    private readonly Dictionary<ClassType, bool> classTaken = new Dictionary<ClassType, bool>();

    // clientId -> spawned character NetworkObject
    private readonly Dictionary<ulong, NetworkObject> clientCharacter = new Dictionary<ulong, NetworkObject>();

    // clientId -> chosen class (only used if you want to free on disconnect)
    private readonly Dictionary<ulong, ClassType> clientClass = new Dictionary<ulong, ClassType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (ClassType t in Enum.GetValues(typeof(ClassType)))
            classTaken[t] = false;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    public bool IsClassAvailable(ClassType type)
    {
        if (!enforceUniqueClasses) return true;

        if (!classTaken.TryGetValue(type, out bool taken)) return true;
        return !taken;
    }

    /// <summary>
    /// Server-only. Spawns the character for clientId if allowed. Returns true on success.
    /// </summary>
    public bool TrySpawnCharacter(ulong clientId, ClassType type)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[LobbyManager] TrySpawnCharacter called on client. This must run on server.");
            return false;
        }

        // One character per client
        if (clientCharacter.ContainsKey(clientId))
        {
            Debug.LogWarning($"[LobbyManager] Client {clientId} already spawned a character.");
            return false;
        }

        // One class per match (if enabled)
        if (enforceUniqueClasses && !IsClassAvailable(type))
        {
            Debug.LogWarning($"[LobbyManager] Class {type} already taken.");
            return false;
        }

        GameObject prefab = GetPrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"[LobbyManager] Missing prefab for class {type}. Assign in inspector.");
            return false;
        }

        Vector3 spawnPos = GetSpawnPosition(clientId);
        spawnPos.z = 0f;
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);


        NetworkObject no = go.GetComponent<NetworkObject>();
        if (no == null)
        {
            Debug.LogError($"[LobbyManager] Prefab {prefab.name} missing NetworkObject.");
            Destroy(go);
            return false;
        }

        // Give ownership to this client
        no.SpawnWithOwnership(clientId, true);

        clientCharacter[clientId] = no;
        clientClass[clientId] = type;

        if (enforceUniqueClasses)
            classTaken[type] = true;

        return true;
    }

    /// <summary>
    /// Backward-compatible wrapper if your older code calls SpawnCharacter().
    /// </summary>
    public void SpawnCharacter(ulong clientId, ClassType type)
    {
        TrySpawnCharacter(clientId, type);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;

        // Despawn the player's character if it exists
        if (clientCharacter.TryGetValue(clientId, out NetworkObject no) && no != null && no.IsSpawned)
        {
            no.Despawn(true);
        }
        clientCharacter.Remove(clientId);

        // Optionally free the class if you want to allow re-picking when someone leaves
        if (freeClassOnDisconnect && clientClass.TryGetValue(clientId, out ClassType type))
        {
            classTaken[type] = false;
        }
        clientClass.Remove(clientId);
    }

    private GameObject GetPrefab(ClassType type)
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

    private Vector3 GetSpawnPosition(ulong clientId)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = (int)(clientId % (ulong)spawnPoints.Length);
            if (spawnPoints[idx] != null)
            {
                Vector3 p = spawnPoints[idx].position;
                p.z = 0f; // FORCE spawn point Z=0
                return p;
            }
        }

        // Fallback: spread along X at Z=0
        int slot = (int)(clientId % 8);
        return new Vector3(slot * 3.0f, 0f, 0f);
    }


    public void ReleaseClass(ClassType type)
    {
        if (!IsServer) return;
        if (!enforceUniqueClasses) return;

        if (classTaken.ContainsKey(type))
            classTaken[type] = false;
    }

}
