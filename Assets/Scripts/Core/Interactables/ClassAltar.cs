using Unity.Netcode;
using UnityEngine;

public class ClassAltar : NetworkBehaviour
{
    [Header("Altar Identity")]
    public ClassType classType;

    [Header("Health")]
    [SerializeField] private int maxHp = 100;

    [Tooltip("How long after last damage before regen starts.")]
    [SerializeField] private float regenDelaySeconds = 5f;

    [Tooltip("HP per second to regenerate once regen starts.")]
    [SerializeField] private float regenPerSecond = 5f;

    [Tooltip("If true, altar regens only after it has been claimed (spawned once).")]
    [SerializeField] private bool regenOnlyWhenClaimed = false;

    [Header("On Destruction")]
    [Tooltip("If true, destroying the altar frees the class (allows another altar or respawn system to re-offer it).")]
    [SerializeField] private bool freeClassWhenDestroyed = false;

    [Header("Visuals")]
    [SerializeField] private GameObject occupiedVisual;   // shown when claimed/empty
    [SerializeField] private GameObject availableVisual;  // shown when available

    // State replicated to all clients
    private readonly NetworkVariable<bool> claimed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> hp = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float lastDamageTime;
    private float regenAccumulator; // fixes FloorToInt regen stalling

    // Optional accessors for UI/other scripts
    public bool IsClaimed => claimed.Value;
    public int HP => hp.Value;
    public int MaxHP => maxHp;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hp.Value = maxHp;
            lastDamageTime = Time.time;
            regenAccumulator = 0f;
        }

        claimed.OnValueChanged += (_, __) => ApplyVisuals();
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        bool isClaimed = claimed.Value;

        if (availableVisual != null) availableVisual.SetActive(!isClaimed);
        if (occupiedVisual != null) occupiedVisual.SetActive(isClaimed);
    }

    private void Update()
    {
        if (!IsServer) return;
        if (hp.Value <= 0) return;

        if (regenOnlyWhenClaimed && !claimed.Value) return;

        if (Time.time - lastDamageTime < regenDelaySeconds) return;
        if (hp.Value >= maxHp) return;

        regenAccumulator += regenPerSecond * Time.deltaTime;

        int add = Mathf.FloorToInt(regenAccumulator);
        if (add <= 0) return;

        regenAccumulator -= add;
        hp.Value = Mathf.Min(maxHp, hp.Value + add);
    }

    // Called by PlayerConnection when clicking the altar
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryUseServerRpc(RpcParams rpcParams = default)
    {
        if (claimed.Value) return;
        if (hp.Value <= 0) return;

        ulong sender = rpcParams.Receive.SenderClientId;

        if (LobbyManager.Instance == null) return;

        bool ok = LobbyManager.Instance.TrySpawnCharacter(sender, classType);
        if (!ok) return;

        claimed.Value = true;
        // visuals update automatically via NetworkVariable change
    }

    // Call this from weapons/attacks on the server
    public void ServerTakeDamage(int dmg)
    {
        if (!IsServer) return;
        if (hp.Value <= 0) return;
        if (dmg <= 0) return;

        hp.Value = Mathf.Max(0, hp.Value - dmg);
        lastDamageTime = Time.time;
        regenAccumulator = 0f;

        if (hp.Value == 0)
        {
            if (freeClassWhenDestroyed && LobbyManager.Instance != null)
            {
                // Only meaningful if you implement ReleaseClass in LobbyManager (see note below).
                LobbyManager.Instance.ReleaseClass(classType);
            }

            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
        }
    }

    public bool ServerTryUse(ulong senderClientId)
    {
        if (!IsServer) return false;

        if (claimed.Value) return false;
        if (hp.Value <= 0) return false;

        if (LobbyManager.Instance == null) return false;

        bool ok = LobbyManager.Instance.TrySpawnCharacter(senderClientId, classType);
        if (!ok) return false;

        claimed.Value = true;
        return true;
    }

}
