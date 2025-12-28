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
        RequestSpawnRpc(0);

        if (selectionUI != null) selectionUI.SetActive(false);
        else gameObject.SetActive(false);
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnRpc(int classIndex, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        GameObject prefab = classIndex switch
        {
            0 => Archer,
            1 => Knight,
            2 => Mage,
            3 => Healer,
            _ => null
        };

        if (prefab == null) return;

        GameObject newCharacter = Instantiate(prefab);

        int slot = (int)(clientId % 8);
        newCharacter.transform.position = new Vector3(slot * 3.0f, 0f, 0f);

        var netObj = newCharacter.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Destroy(newCharacter);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId);
    }
}




