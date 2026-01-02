using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

// Server-authoritative stats container.
//
// Key goals:
// - Data-driven stats per prefab via StatsDefinition (ScriptableObject)
// - Optional spawn-time override: assign a definition + starting level before NetworkObject.Spawn()
// - Preserve existing XP/level-up and ability-point spend workflow

public class stats : NetworkBehaviour
{
    [Header("Definition (Recommended)")]
    [Tooltip("If assigned, this drives base stats, growth, special combat traits, and ability-point spend rules.")]
    [SerializeField] private StatsDefinition definition;

    [Tooltip("If definition is null, auto-resolve from Resources/StatsDefinitions based on ClassIdentity/monster type.")]
    [SerializeField] private bool autoResolveDefinition = true;

    [Header("Starting Level")]
    [Tooltip("If false, starting level comes from the StatsDefinition (defaultStartLevel). If true, uses Overridden Start Level.")]
    [SerializeField] private bool overrideStartLevel = false;

    [Tooltip("Used only when Override Start Level is enabled.")]
    [FormerlySerializedAs("startLevel")]
    [SerializeField] private int overriddenStartLevel = 1;

    public StatsDefinition Definition => definition;

    [Header("UI")]
    public HealthBar healthBar;

    // ----------------------------
    // Legacy prefab defaults (only used when no definition can be resolved).
    // ----------------------------

    [Header("Legacy Base Stats (Prefab Defaults)")]
    public int HP = 10;          // default Max HP on spawn
    public int defense = 0;
    public int mana = 0;
    public int baseDamage = 1;
    public int bonusDamage = 0;  // flat bonus damage (items/buffs)
    public int level = 1;
    public int abilityPoints = 0;
    public int skillPoints = 1;
    public float moveSpeed = 5f;

    public event System.Action<stats> OnDiedServer;
    private bool m_deathSignaled = false;

    [Header("Progression")]
    [SerializeField] private int abilityPointsPerLevelLegacy = 5;

    // ----------------------------
    // Networked stats
    // ----------------------------

    public NetworkVariable<int> Level = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Damage = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> BonusDamage = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

    public NetworkVariable<int> MaxHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("XP (Progression)")]
    [SerializeField] private int xpToNextDefault = 10;

    public NetworkVariable<int> XP = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> XPToNext = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ----------------------------
    // Spawn configuration
    // ----------------------------

    /// <summary>
    /// Server-only: set stats definition + starting level BEFORE NetworkObject.Spawn().
    /// If def is null, definition will be auto-resolved (if enabled) based on context.
    /// </summary>
    public void ServerConfigure(StatsDefinition def, int startingLevel)
    {
        if (!IsServer)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[stats] ServerConfigure called on client; ignoring.");
#endif
            return;
        }

        definition = def;

        // Preserve old behavior where <=0 meant "use default".
        if (startingLevel <= 0)
        {
            overrideStartLevel = false;
            overriddenStartLevel = 1;
        }
        else
        {
            overrideStartLevel = true;
            overriddenStartLevel = startingLevel;
        }
    }

    /// <summary>
    /// Server-only: set the starting level BEFORE Spawn() (definition unchanged).
    /// </summary>
    public void ServerSetStartLevel(int startingLevel)
    {
        if (!IsServer) return;

        // Preserve old behavior where <=0 meant "use default".
        if (startingLevel <= 0)
        {
            overrideStartLevel = false;
            overriddenStartLevel = 1;
        }
        else
        {
            overrideStartLevel = true;
            overriddenStartLevel = startingLevel;
        }
    }

    public override void OnNetworkSpawn()
    {
        // Resolve definition on BOTH server and clients (so UI can read rules even if prefab left it null).
        if (definition == null && autoResolveDefinition)
            definition = ResolveDefinitionFromContext();

        if (IsServer)
        {
            ServerInitializeAuthoritativeValues();
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

    private void ServerInitializeAuthoritativeValues()
    {
        // Decide starting level
        int lvl;

        if (overrideStartLevel)
        {
            lvl = overriddenStartLevel;
        }
        else
        {
            // Default: definition decides. If no definition, fall back to legacy `level`.
            lvl = (definition != null) ? definition.defaultStartLevel : level;
        }

        lvl = Mathf.Max(1, lvl);
        Level.Value = lvl;

        if (definition != null)
        {
            MaxHP.Value = definition.maxHp.EvaluateAtLevel(lvl);
            Damage.Value = definition.damage.EvaluateAtLevel(lvl);
            Defense.Value = definition.defense.EvaluateAtLevel(lvl);
            Mana.Value = definition.mana.EvaluateAtLevel(lvl);
            MoveSpeed.Value = definition.moveSpeed.EvaluateAtLevel(lvl);
            BonusDamage.Value = Mathf.Max(0, definition.baseBonusDamage);
        }
        else
        {
            // Legacy: use prefab fields.
            MaxHP.Value = Mathf.Max(1, HP);
            Damage.Value = Mathf.Max(1, baseDamage);
            Defense.Value = Mathf.Max(0, defense);
            Mana.Value = Mathf.Max(0, mana);
            MoveSpeed.Value = Mathf.Max(0.01f, moveSpeed);
            BonusDamage.Value = Mathf.Max(0, bonusDamage);
        }

        CurrentHP.Value = MaxHP.Value;

        AbilityPoints.Value = Mathf.Max(0, abilityPoints);
        SkillPoints.Value = Mathf.Max(0, skillPoints);

        XP.Value = 0;
        XPToNext.Value = Mathf.Max(1, xpToNextDefault);
    }

    private StatsDefinition ResolveDefinitionFromContext()
    {
        // Monsters
        if (GetComponent<Slime>() != null)
        {
            var slime = StatsDefinitionLibrary.GetForSlime();
            if (slime != null) return slime;
        }

        // Player classes
        var ident = GetComponent<ClassIdentity>();
        if (ident != null)
        {
            var byClass = StatsDefinitionLibrary.GetForClass(ident.classType);
            if (byClass != null) return byClass;
        }

        return StatsDefinitionLibrary.GetGeneric();
    }

    // ----------------------------
    // Combat API (server authoritative)
    // ----------------------------

    // Server-only. Apply raw damage.
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

        // Optional: single-hit clamp (Knight rule) driven by definition.
        if (definition != null && definition.enableSingleHitClamp)
        {
            int cur = Mathf.Max(0, CurrentHP.Value);
            if (cur > 2)
            {
                int maxAllowed = Mathf.FloorToInt(cur * definition.clampPercentOfCurrentHp) - definition.clampMinus;
                if (maxAllowed > 0 && damageReceived > maxAllowed)
                    damageReceived = maxAllowed;
            }

            // Safety: never allow a no-op hit due to clamp math.
            damageReceived = Mathf.Max(1, damageReceived);
        }

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
        ServerApplyLevelUp();
    }

    // Applies: Level +1, applies growth rules, refills HP (optionally), grants ability points.
    public void ServerApplyLevelUp()
    {
        if (!IsServer) return;

        Level.Value = Mathf.Max(1, Level.Value + 1);

        if (definition != null)
        {
            MaxHP.Value = definition.maxHp.ApplyNextLevel(MaxHP.Value);
            Damage.Value = definition.damage.ApplyNextLevel(Damage.Value);
            Defense.Value = definition.defense.ApplyNextLevel(Defense.Value);
            Mana.Value = definition.mana.ApplyNextLevel(Mana.Value);
            MoveSpeed.Value = definition.moveSpeed.ApplyNextLevel(MoveSpeed.Value);

            AbilityPoints.Value += Mathf.Max(0, definition.abilityPointsPerLevel);

            if (definition.refillHpToFullOnLevelUp)
                CurrentHP.Value = MaxHP.Value;
            else
                CurrentHP.Value = Mathf.Min(CurrentHP.Value, MaxHP.Value);
        }
        else
        {
            // Legacy: +10% to core stats.
            MaxHP.Value = Mathf.Max(1, Mathf.CeilToInt(MaxHP.Value * 1.10f));
            Damage.Value = Mathf.Max(1, Mathf.CeilToInt(Damage.Value * 1.10f));
            Defense.Value = Mathf.Max(0, Mathf.CeilToInt(Defense.Value * 1.10f));
            Mana.Value = Mathf.Max(0, Mathf.CeilToInt(Mana.Value * 1.10f));
            MoveSpeed.Value = Mathf.Max(0.01f, MoveSpeed.Value * 1.10f);

            CurrentHP.Value = MaxHP.Value;
            AbilityPoints.Value += Mathf.Max(0, abilityPointsPerLevelLegacy);
        }
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

        if (definition != null)
        {
            if (deltaDamage != 0)
                Damage.Value = definition.spendDamage.PreviewDelta(Damage.Value, Mathf.Max(0, deltaDamage));

            if (deltaMaxHp != 0)
            {
                MaxHP.Value = definition.spendMaxHp.PreviewDelta(MaxHP.Value, Mathf.Max(0, deltaMaxHp));
                CurrentHP.Value = Mathf.Min(CurrentHP.Value, MaxHP.Value);
            }

            if (deltaDefense != 0)
                Defense.Value = definition.spendDefense.PreviewDelta(Defense.Value, deltaDefense);

            if (deltaMana != 0)
                Mana.Value = definition.spendMana.PreviewDelta(Mana.Value, deltaMana);

            if (Mathf.Abs(deltaMoveSpeed) > 0.00001f)
                MoveSpeed.Value = definition.spendMoveSpeed.PreviewDelta(MoveSpeed.Value, deltaMoveSpeed);
        }
        else
        {
            if (deltaDamage != 0) Damage.Value = Mathf.Max(1, Damage.Value + deltaDamage);
            if (deltaMaxHp != 0)
            {
                MaxHP.Value = Mathf.Max(1, MaxHP.Value + deltaMaxHp);
                CurrentHP.Value = Mathf.Min(CurrentHP.Value, MaxHP.Value);
            }

            if (deltaDefense != 0) Defense.Value = Mathf.Max(0, Defense.Value + deltaDefense);
            if (deltaMana != 0) Mana.Value = Mathf.Max(0, Mana.Value + deltaMana);
            if (Mathf.Abs(deltaMoveSpeed) > 0.00001f) MoveSpeed.Value = Mathf.Max(0.01f, MoveSpeed.Value + deltaMoveSpeed);
        }
    }

    // Used by altar restore (server-only)
    public void ServerApplySnapshot(
        int snapLevel,
        int snapMaxHp,
        int snapDamage,
        int snapBonusDamage,
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
        BonusDamage.Value = Mathf.Max(0, snapBonusDamage);
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

        if (!m_deathSignaled)
        {
            m_deathSignaled = true;
            OnDiedServer?.Invoke(this);
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    public void ServerAddXp(int amount)
    {
        if (!IsServer) return;
        if (amount <= 0) return;

        XP.Value += amount;

        while (XP.Value >= XPToNext.Value)
        {
            XP.Value -= XPToNext.Value;
            ServerApplyLevelUp();
            XPToNext.Value = CalcXpToNext(Level.Value);
        }
    }

    private int CalcXpToNext(int currentLevel)
    {
        float baseVal = Mathf.Max(1, xpToNextDefault);
        float scaled = baseVal * Mathf.Pow(1.15f, Mathf.Max(0, currentLevel - 1));
        return Mathf.Max(1, Mathf.CeilToInt(scaled));
    }
}
