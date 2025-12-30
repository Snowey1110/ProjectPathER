using Unity.Netcode;
using UnityEngine;


// Server-authoritative stats container.
// - Core combat/progression stats are NetworkVariables (server writes, everyone reads).
// - The public fields under "Base Stats" are prefab defaults (used only to initialize server-side values on spawn).

public class stats : NetworkBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Base Stats (Prefab Defaults)")]
    public int HP = 10;          // default Max HP on spawn
    public int defense = 0;
    public int mana = 0;
    public int baseDamage = 1;
    public int level = 1;
    public int abilityPoints = 0;
    public int skillPoints = 1;
    public float moveSpeed = 5f;

    [Header("Progression")]
    [SerializeField] private int abilityPointsPerLevel = 5;

    // ----------------------------
    // Networked stats
    // ----------------------------

    public NetworkVariable<int> Level = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Damage = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Defense = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Mana = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> MoveSpeed = new NetworkVariable<float>(
        5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> AbilityPoints = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> SkillPoints = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Networked HP (server writes, everyone reads)
    public NetworkVariable<int> MaxHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize authoritative values from prefab defaults
            Level.Value = Mathf.Max(1, level);

            MaxHP.Value = Mathf.Max(1, HP);
            CurrentHP.Value = MaxHP.Value;

            Damage.Value = Mathf.Max(1, baseDamage);
            Defense.Value = Mathf.Max(0, defense);
            Mana.Value = Mathf.Max(0, mana);
            MoveSpeed.Value = Mathf.Max(0.01f, moveSpeed);

            AbilityPoints.Value = Mathf.Max(0, abilityPoints);
            SkillPoints.Value = Mathf.Max(0, skillPoints);
        }

        // Auto-find health bar if not assigned in inspector
        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBar>(true);

        MaxHP.OnValueChanged += OnMaxHpChanged;
        CurrentHP.OnValueChanged += OnHpChanged;

        if (healthBar != null)
        {
            healthBar.Bind(this);

            // Auto-set follow target if your bar uses HealthBarFollow
            var follow = healthBar.GetComponentInParent<HealthBarFollow>();
            if (follow != null && follow.objectToFollow == null)
                follow.objectToFollow = transform;

            healthBar.SetMaxHealth(MaxHP.Value, CurrentHP.Value);
            healthBar.SetHealth(CurrentHP.Value);
        }
    }

    public override void OnDestroy()
    {
        MaxHP.OnValueChanged -= OnMaxHpChanged;
        CurrentHP.OnValueChanged -= OnHpChanged;

        base.OnDestroy();
    }

    private void OnMaxHpChanged(int oldV, int newV)
    {
        if (healthBar != null)
            healthBar.SetMaxHealth(newV, CurrentHP.Value);
    }

    private void OnHpChanged(int oldV, int newV)
    {
        if (healthBar != null)
            healthBar.SetHealth(newV);
    }

    // ----------------------------
    // Combat API (server authoritative)
    // ----------------------------

    // Server-only. Apply raw damage (you can incorporate defense externally if desired).
    public void takeDamage(int damageReceived)
    {
        if (!IsServer)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[stats] takeDamage called on client for '{name}'. Ignored (server-authoritative).");
#endif
            return;
        }

        if (damageReceived <= 0) return;

        int newHp = Mathf.Max(0, CurrentHP.Value - damageReceived);
        CurrentHP.Value = newHp;

        if (newHp <= 0)
            DieServer();
    }

    // Server-only heal.
    public void Heal(int amount)
    {
        if (!IsServer)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[stats] Heal called on client for '{name}'. Ignored (server-authoritative).");
#endif
            return;
        }

        if (amount <= 0) return;
        CurrentHP.Value = Mathf.Min(MaxHP.Value, CurrentHP.Value + amount);
    }

    // Server-only setter if you need to apply loadouts/saves
    public void ServerSetMaxHpAndFill(int newMaxHp)
    {
        if (!IsServer) return;

        MaxHP.Value = Mathf.Max(1, newMaxHp);
        CurrentHP.Value = MaxHP.Value;
    }

    // ----------------------------
    // Progression (server authoritative)
    // ----------------------------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestLevelUpServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ServerApplyLevelUp10Percent();
    }

    // Applies: Level +1, and increases key stats by 10% (compounding).
    // Also refills HP to full and grants ability points.
    public void ServerApplyLevelUp10Percent()
    {
        if (!IsServer) return;

        Level.Value = Mathf.Max(1, Level.Value + 1);

        MaxHP.Value = Mathf.Max(1, Mathf.CeilToInt(MaxHP.Value * 1.10f));
        Damage.Value = Mathf.Max(1, Mathf.CeilToInt(Damage.Value * 1.10f));
        Defense.Value = Mathf.Max(0, Mathf.CeilToInt(Defense.Value * 1.10f));
        Mana.Value = Mathf.Max(0, Mathf.CeilToInt(Mana.Value * 1.10f));
        MoveSpeed.Value = Mathf.Max(0.01f, MoveSpeed.Value * 1.10f);

        // Full heal on level up
        CurrentHP.Value = MaxHP.Value;

        AbilityPoints.Value += Mathf.Max(0, abilityPointsPerLevel);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SpendAbilityPointsServerRpc(
        int deltaDamage,
        int deltaDefense,
        int deltaMaxHp,
        int deltaMana,
        float deltaMoveSpeed,
        int cost,
        RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        if (cost <= 0) return;
        if (AbilityPoints.Value < cost) return;

        AbilityPoints.Value -= cost;

        if (deltaDamage != 0) Damage.Value = Mathf.Max(1, Damage.Value + deltaDamage);
        if (deltaDefense != 0) Defense.Value = Mathf.Max(0, Defense.Value + deltaDefense);
        if (deltaMana != 0) Mana.Value = Mathf.Max(0, Mana.Value + deltaMana);
        if (deltaMoveSpeed != 0f) MoveSpeed.Value = Mathf.Max(0.01f, MoveSpeed.Value + deltaMoveSpeed);

        if (deltaMaxHp != 0)
        {
            MaxHP.Value = Mathf.Max(1, MaxHP.Value + deltaMaxHp);
            CurrentHP.Value = Mathf.Min(CurrentHP.Value, MaxHP.Value);
        }
    }

    // Used by altar restore (server-only)
    public void ServerApplySnapshot(
        int snapLevel,
        int snapMaxHp,
        int snapDamage,
        int snapDefense,
        int snapMana,
        float snapMoveSpeed,
        int snapAbilityPoints,
        int snapSkillPoints,
        bool fillHp)
    {
        if (!IsServer) return;

        Level.Value = Mathf.Max(1, snapLevel);
        MaxHP.Value = Mathf.Max(1, snapMaxHp);
        Damage.Value = Mathf.Max(1, snapDamage);
        Defense.Value = Mathf.Max(0, snapDefense);
        Mana.Value = Mathf.Max(0, snapMana);
        MoveSpeed.Value = Mathf.Max(0.01f, snapMoveSpeed);
        AbilityPoints.Value = Mathf.Max(0, snapAbilityPoints);
        SkillPoints.Value = Mathf.Max(0, snapSkillPoints);

        if (fillHp)
            CurrentHP.Value = MaxHP.Value;
        else
            CurrentHP.Value = Mathf.Clamp(CurrentHP.Value, 0, MaxHP.Value);
    }

    private void DieServer()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
