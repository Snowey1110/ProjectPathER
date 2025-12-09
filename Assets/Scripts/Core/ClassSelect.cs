using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;




public class ClassSelect : NetworkBehaviour
{
    public GameObject Archer;
    public GameObject Knight;
    public GameObject Mage;
    public GameObject Healer;
    public Camera MainCamera;

    [Header("UI References")]
    public GameObject selectionUI;
    public void selectArcher()
    {
        // We ask the Server to do the work.
        // We pass our own ID so the server knows who to give the object to.
        RequestSpawnServerRpc(0, NetworkManager.Singleton.LocalClientId);

        // Hide the menu immediately for the local player
        if (selectionUI != null) selectionUI.SetActive(false);
        else gameObject.SetActive(false); // Fallback if you didn't assign UI
    }

    [ServerRpc(RequireOwnership = false)] // "False" means a client (who doesn't own this object yet) can call it.
    private void RequestSpawnServerRpc(int classIndex, ulong clientId)
    {
        GameObject newCharacter = null;

        // Pick the correct prefab
        switch (classIndex)
        {
            case 0: newCharacter = Instantiate(Archer); break;
            case 1: newCharacter = Instantiate(Knight); break;
            case 2: newCharacter = Instantiate(Mage); break;
            case 3: newCharacter = Instantiate(Healer); break;
        }

        // Spawn it on the Network
        if (newCharacter != null)
        {
            // This turns the "Local GameObject" into a "Networked Object"
            // "SpawnAsPlayerObject" automatically sets "IsOwner = true" for that specific client.
            newCharacter.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
    }
}




