using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Class Prefabs")]
    public GameObject archerPrefab;
    public GameObject knightPrefab;
    public GameObject magePrefab;
    public GameObject healerPrefab;

    // Track which classes are currently alive/taken
    private Dictionary<ClassType, bool> classTakenStatus = new Dictionary<ClassType, bool>();

    private void Awake()
    {
        Instance = this;
        // Initialize all classes as "Available" (False = not taken)
        classTakenStatus.Add(ClassType.Archer, false);
        classTakenStatus.Add(ClassType.Knight, false);
        classTakenStatus.Add(ClassType.Mage, false);
        classTakenStatus.Add(ClassType.Healer, false);
    }

    public bool IsClassAvailable(ClassType type)
    {
        return !classTakenStatus[type];
    }

    public void SpawnCharacter(ulong clientId, ClassType type)
    {
        GameObject prefabToSpawn = null;
        Vector3 spawnPos = Vector3.zero;

        switch (type)
        {
            case ClassType.Archer: prefabToSpawn = archerPrefab; break;
            case ClassType.Knight: prefabToSpawn = knightPrefab; break;
            case ClassType.Mage: prefabToSpawn = magePrefab; break;
            case ClassType.Healer: prefabToSpawn = healerPrefab; break;
        }

        if (prefabToSpawn != null)
        {
            // Instantiate on Server
            GameObject character = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // Spawn on Network and GIVE OWNERSHIP to the client who clicked
            character.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

            // Mark class as taken
            classTakenStatus[type] = true;

        }
    }

    // Call this when a player dies or disconnects to free up the class
    public void ReleaseClass(ClassType type)
    {
        classTakenStatus[type] = false;
    }
}