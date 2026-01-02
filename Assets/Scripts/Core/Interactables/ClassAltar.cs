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

    [Tooltip("If true, destroying this altar frees its class in LobbyManager (unique-classes rule).")]
    [SerializeField] private bool freeClassWhenDestroyed = false;

    [Header("Visuals (optional)")]
    [SerializeField] private GameObject availableVisual;
    [SerializeField] private GameObject occupiedVisual;

    private readonly NetworkVariable<bool> claimed = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> hp = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Saved progression (persisted on server in the altar when a player disconnects)
    private readonly NetworkVariable<bool> hasSavedProgress = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedLevel = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedMaxHp = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedDamage = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedBonusDamage = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedDefense = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedMana = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> savedMoveSpeed = new NetworkVariable<float>(
        5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedAbilityPoints = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> savedSkillPoints = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float lastDamageTime;
    private float regenAccumulator;

    public bool IsClaimed => claimed.Value;
    public bool HasSavedProgress => hasSavedProgress.Value;

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
        if (regenAccumulator < 1f) return;

        int add = Mathf.FloorToInt(regenAccumulator);
        regenAccumulator -= add;
        hp.Value = Mathf.Min(maxHp, hp.Value + add);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryUseServerRpc(RpcParams rpcParams = default)
    {
        if (claimed.Value) return;
        if (hp.Value <= 0) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (LobbyManager.Instance == null) return;

        bool ok = LobbyManager.Instance.TrySpawnCharacterFromAltar(sender, classType, this);
        if (!ok) return;

        claimed.Value = true;
    }

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
                LobbyManager.Instance.ReleaseClass(classType);

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

        bool ok = LobbyManager.Instance.TrySpawnCharacterFromAltar(senderClientId, classType, this);
        if (!ok) return false;

        claimed.Value = true;
        return true;
    }

    // ---------------------------
    // Progress persistence
    // ---------------------------

    public void ServerSaveProgressFromStats(stats st)
    {
        if (!IsServer) return;
        if (st == null) return;

        hasSavedProgress.Value = true;
        savedLevel.Value = st.Level.Value;
        savedMaxHp.Value = st.MaxHP.Value;
        savedDamage.Value = st.Damage.Value;
        savedBonusDamage.Value = st.BonusDamage.Value;
        savedDefense.Value = st.Defense.Value;
        savedMana.Value = st.Mana.Value;
        savedMoveSpeed.Value = st.MoveSpeed.Value;
        savedAbilityPoints.Value = st.AbilityPoints.Value;
        savedSkillPoints.Value = st.SkillPoints.Value;
    }

    public void ServerApplySavedProgressToStats(stats st, bool fillHp)
    {
        if (!IsServer) return;
        if (st == null) return;
        if (!hasSavedProgress.Value) return;

        st.ServerApplySnapshot(
            savedLevel.Value,
            savedMaxHp.Value,
            savedDamage.Value,
            savedBonusDamage.Value,
            savedDefense.Value,
            savedMana.Value,
            savedMoveSpeed.Value,
            savedAbilityPoints.Value,
            savedSkillPoints.Value,
            fillHp);
    }

    public void ServerUnclaim()
    {
        if (!IsServer) return;
        claimed.Value = false;
    }
}
