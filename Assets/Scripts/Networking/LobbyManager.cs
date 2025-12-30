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

    [Tooltip("Legacy toggle. Disconnect now also frees the altar by default; this flag only controls whether classTaken is cleared when no altar is known.")]
    [SerializeField] private bool freeClassOnDisconnect = false;

    private readonly Dictionary<ClassType, bool> classTaken = new Dictionary<ClassType, bool>();
    private readonly Dictionary<ulong, NetworkObject> clientCharacter = new Dictionary<ulong, NetworkObject>();
    private readonly Dictionary<ulong, ClassType> clientClass = new Dictionary<ulong, ClassType>();

    // clientId -> altar used to spawn (if spawned via an altar)
    private readonly Dictionary<ulong, ClassAltar> clientAltar = new Dictionary<ulong, ClassAltar>();

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

    public bool TrySpawnCharacter(ulong clientId, ClassType type)
    {
        return TrySpawnCharacterInternal(clientId, type, null, out _);
    }

    public bool TrySpawnCharacterFromAltar(ulong clientId, ClassType type, ClassAltar altar)
    {
        return TrySpawnCharacterInternal(clientId, type, altar, out _);
    }

    private bool TrySpawnCharacterInternal(ulong clientId, ClassType type, ClassAltar altar, out NetworkObject spawned)
    {
        spawned = null;

        if (!IsServer)
        {
            Debug.LogWarning("[LobbyManager] TrySpawnCharacter called on client. This must run on server.");
            return false;
        }

        if (clientCharacter.ContainsKey(clientId))
        {
            Debug.LogWarning($"[LobbyManager] Client {clientId} already spawned a character.");
            return false;
        }

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

        no.SpawnWithOwnership(clientId, true);

        spawned = no;
        clientCharacter[clientId] = no;
        clientClass[clientId] = type;

        if (enforceUniqueClasses)
            classTaken[type] = true;

        if (altar != null)
        {
            clientAltar[clientId] = altar;

            if (altar.HasSavedProgress)
            {
                var st = no.GetComponent<stats>();
                if (st != null)
                    altar.ServerApplySavedProgressToStats(st, fillHp: true);
            }
        }

        return true;
    }

    public void SpawnCharacter(ulong clientId, ClassType type)
    {
        TrySpawnCharacter(clientId, type);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;

        // Save stats into the altar and free the altar if this client spawned from one.
        if (clientCharacter.TryGetValue(clientId, out NetworkObject no) && no != null)
        {
            if (clientAltar.TryGetValue(clientId, out ClassAltar altar) && altar != null)
            {
                var st = no.GetComponent<stats>();
                if (st != null)
                    altar.ServerSaveProgressFromStats(st);

                altar.ServerUnclaim();
            }
        }

        // Despawn character
        if (clientCharacter.TryGetValue(clientId, out NetworkObject spawnedNo) && spawnedNo != null && spawnedNo.IsSpawned)
            spawnedNo.Despawn(true);

        clientCharacter.Remove(clientId);

        // Free class when disconnecting if:
        // - spawned from an altar (so the altar can be reused), OR
        // - legacy flag is enabled.
        if (clientClass.TryGetValue(clientId, out ClassType type))
        {
            bool shouldFree = clientAltar.ContainsKey(clientId) || freeClassOnDisconnect;
            if (shouldFree && enforceUniqueClasses)
                classTaken[type] = false;
        }

        clientClass.Remove(clientId);
        clientAltar.Remove(clientId);
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
                p.z = 0f;
                return p;
            }
        }

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
