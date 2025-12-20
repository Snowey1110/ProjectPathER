using Unity.Netcode;
using UnityEngine;

public class ArcherAttack : BaseAttack
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    [SerializeField] private float spawnOffset = 1.5f;

    public override void Fire(Vector2 direction)
    {
        if (!IsOwner) return;

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;

        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        Vector3 spawnPos = transform.position + (Vector3)direction * spawnOffset;

        SpawnArrowServerRpc(direction, spawnPos, NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnArrowServerRpc(Vector2 dir, Vector3 spawnPos, ulong shooterId, ServerRpcParams rpcParams = default)
    {
        // Security: only allow the owner of THIS attack object to shoot
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        if (arrowPrefab == null)
        {
            Debug.LogError("[ArcherAttack] Server: arrowPrefab is null.");
            return;
        }

        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        var arrowNo = arrowGo.GetComponent<NetworkObject>();
        if (arrowNo == null)
        {
            Debug.LogError("[ArcherAttack] Arrow prefab missing NetworkObject.");
            Destroy(arrowGo);
            return;
        }

        int dmg = 1;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out var shooterNo))
        {
            var st = shooterNo.GetComponent<stats>();
            if (st != null) dmg = st.baseDamage;
        }

        arrowNo.Spawn(true);

        var arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
            arrow.ServerInit(dir, arrowSpeed, dmg);
    }
}
