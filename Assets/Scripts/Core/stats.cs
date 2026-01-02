using Unity.Netcode;
using UnityEngine;


// Server-authoritative stats container.
// - Core combat/progression stats are NetworkVariables (server writes, everyone reads).
// - The public fields under "Base Stats" are prefab defaults (used only to initialize server-side values on spawn).

public class stats : NetworkBehaviour
{
    private enum StatsProfile
    {
        Generic = 0,
        Archer = 1,
        Knight = 2,
        Slime = 3,
        Mage = 4,
        Healer = 5,
    }

    private StatsProfile _profile = StatsProfile.Generic;

    [Header("UI")]
    public HealthBar healthBar;

    [Header("Base Stats (Prefab Defaults)")]
    public int HP = 10;          // default Max HP on spawn
    public int defense = 0;
    public int mana = 0;
    public int baseDamage = 1;
    public int bonusDamage = 0;  // flat bonus damage (used by Archer)
    public int level = 1;
    public int abilityPoints = 0;
    public int skillPoints = 1;
    public float moveSpeed = 5f;

    public event System.Action<stats> OnDiedServer;
    private bool m_deathSignaled = false;

    [Header("Progression")]
    [SerializeField] private int abilityPointsPerLevel = 5;

    // ----------------------------
    // Networked stats
    // ----------------------------

    public NetworkVariable<int> Level = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Damage = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Flat bonus damage (used by Archer). Default 0 for other units.
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

    // Networked HP (server writes, everyone reads)
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

    public override void OnNetworkSpawn()
    {
        // Cache profile on both server and clients (combat rules need it on server).
        _profile = ResolveProfile();

        if (IsServer)
        {
            // Initialize authoritative values from prefab defaults (or class rules)
            Level.Value = Mathf.Max(1, level);

            switch (_profile)
            {
                case StatsProfile.Archer:
                {
                    // Archer: base HP 30, base damage 10. Level-up adds +5 HP/+5 damage.
                    int startLevel = Level.Value;
                    MaxHP.Value = Mathf.Max(1, 30 + (startLevel - 1) * 5);
                    Damage.Value = Mathf.Max(1, 10 + (startLevel - 1) * 5);
                    BonusDamage.Value = Mathf.Max(0, bonusDamage);
                    break;
                }
                                case StatsProfile.Mage:
                {
                    // Mage: same as Archer (base HP 30, base damage 10, +5/+5 per level).
                    int startLevel = Level.Value;
                    MaxHP.Value = Mathf.Max(1, 30 + (startLevel - 1) * 5);
                    Damage.Value = Mathf.Max(1, 10 + (startLevel - 1) * 5);
                    BonusDamage.Value = Mathf.Max(0, bonusDamage);
                    break;
                }
                case StatsProfile.Healer:
                {
                    // Healer: base HP 50, base damage 5. Level-up: +1 damage, HP stays 50.
                    int startLevel = Level.Value;
                    MaxHP.Value = 50;
                    Damage.Value = Mathf.Max(1, 5 + (startLevel - 1) * 1);
                    BonusDamage.Value = 0;
                    break;
                }
case StatsProfile.Knight:
                {
                    // Knight: base HP 100 (+10/level), base damage 10 (+2/level).
                    int startLevel = Level.Value;
                    MaxHP.Value = Mathf.Max(1, 100 + (startLevel - 1) * 10);
                    Damage.Value = Mathf.Max(1, 10 + (startLevel - 1) * 2);
                    BonusDamage.Value = 0;
                    break;
                }
                case StatsProfile.Slime:
                {
                    // Slime: base HP 20, base attack 10.
                    // Level-up: HP +50% (x1.5), attack +100% (x2.0).
                    int startLevel = Level.Value;

                    float hp = 20f;
                    float atk = 10f;
                    for (int i = 1; i < startLevel; i++)
                    {
                        hp *= 1.5f;
                        atk *= 2.0f;
                    }

                    MaxHP.Value = Mathf.Max(1, Mathf.CeilToInt(hp));
                    Damage.Value = Mathf.Max(1, Mathf.CeilToInt(atk));
                    BonusDamage.Value = 0;
                    break;
                }
                default:
                {
                    MaxHP.Value = Mathf.Max(1, HP);
                    Damage.Value = Mathf.Max(1, baseDamage);
                    BonusDamage.Value = Mathf.Max(0, bonusDamage);
                    break;
                }
            }

            CurrentHP.Value = MaxHP.Value;

            Defense.Value = Mathf.Max(0, defense);
            Mana.Value = Mathf.Max(0, mana);
            MoveSpeed.Value = Mathf.Max(0.01f, moveSpeed);

            AbilityPoints.Value = Mathf.Max(0, abilityPoints);
            SkillPoints.Value = Mathf.Max(0, skillPoints);

            XP.Value = 0;
            XPToNext.Value = Mathf.Max(1, xpToNextDefault);
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

        // Knight: if a single hit would take more than ~50% of CURRENT HP,
        // clamp it to (50% - 1) of current HP.
        // Example: current HP 100 -> max hit 49.
        if (_profile == StatsProfile.Knight)
        {
            int cur = Mathf.Max(0, CurrentHP.Value);
            if (cur > 2)
            {
                int maxAllowed = Mathf.FloorToInt(cur * 0.5f) - 1;
                if (maxAllowed > 0 && damageReceived > maxAllowed)
                    damageReceived = maxAllowed;
            }

            // Safety: never allow a "no-op" hit due to clamp math.
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

        switch (_profile)
        {
            case StatsProfile.Archer:
            case StatsProfile.Mage:
                // Archer/Mage: +5 base damage, +5 base HP per level.
                MaxHP.Value = Mathf.Max(1, MaxHP.Value + 5);
                Damage.Value = Mathf.Max(1, Damage.Value + 5);
                CurrentHP.Value = MaxHP.Value;
                AbilityPoints.Value += Mathf.Max(0, abilityPointsPerLevel);
                break;

            case StatsProfile.Healer:
                // Healer: +1 base damage per level. MaxHP does not increase on level-up.
                Damage.Value = Mathf.Max(1, Damage.Value + 1);
                CurrentHP.Value = MaxHP.Value;
                AbilityPoints.Value += Mathf.Max(0, abilityPointsPerLevel);
                break;

            case StatsProfile.Knight:
                // Knight: +2 base damage, +10 base HP per level.
                MaxHP.Value = Mathf.Max(1, MaxHP.Value + 10);
                Damage.Value = Mathf.Max(1, Damage.Value + 2);
                CurrentHP.Value = MaxHP.Value;
                AbilityPoints.Value += Mathf.Max(0, abilityPointsPerLevel);
                break;

            case StatsProfile.Slime:
                // Slime: HP +50% (x1.5), Attack +100% (x2.0).
                MaxHP.Value = Mathf.Max(1, Mathf.CeilToInt(MaxHP.Value * 1.5f));
                Damage.Value = Mathf.Max(1, Mathf.CeilToInt(Damage.Value * 2.0f));
                CurrentHP.Value = MaxHP.Value;
                break;

            default:
                // Default behavior (legacy): +10% to core stats.
                MaxHP.Value = Mathf.Max(1, Mathf.CeilToInt(MaxHP.Value * 1.10f));
                Damage.Value = Mathf.Max(1, Mathf.CeilToInt(Damage.Value * 1.10f));
                Defense.Value = Mathf.Max(0, Mathf.CeilToInt(Defense.Value * 1.10f));
                Mana.Value = Mathf.Max(0, Mathf.CeilToInt(Mana.Value * 1.10f));
                MoveSpeed.Value = Mathf.Max(0.01f, MoveSpeed.Value * 1.10f);

                CurrentHP.Value = MaxHP.Value;
                AbilityPoints.Value += Mathf.Max(0, abilityPointsPerLevel);
                break;
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

        if (_profile == StatsProfile.Archer || _profile == StatsProfile.Mage)
        {
            // Archer-specific spending rules:
            // - Damage points: +10 base damage per point
            // - HP points: +10% MaxHP per point (compounding)
            if (deltaDamage > 0)
                Damage.Value = Mathf.Max(1, Damage.Value + deltaDamage * 10);

            if (deltaMaxHp > 0)
            {
                for (int i = 0; i < deltaMaxHp; i++)
                    MaxHP.Value = Mathf.Max(1, Mathf.CeilToInt(MaxHP.Value * 1.10f));

                CurrentHP.Value = Mathf.Min(CurrentHP.Value, MaxHP.Value);
            }
        }
        else
        {
            if (deltaDamage != 0) Damage.Value = Mathf.Max(1, Damage.Value + deltaDamage);

            if (deltaMaxHp != 0)
            {
                MaxHP.Value = Mathf.Max(1, MaxHP.Value + deltaMaxHp);
                CurrentHP.Value = Mathf.Min(CurrentHP.Value, MaxHP.Value);
            }
        }

        if (deltaDefense != 0) Defense.Value = Mathf.Max(0, Defense.Value + deltaDefense);
        if (deltaMana != 0) Mana.Value = Mathf.Max(0, Mana.Value + deltaMana);
        if (deltaMoveSpeed != 0f) MoveSpeed.Value = Mathf.Max(0.01f, MoveSpeed.Value + deltaMoveSpeed);
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

        // Fire death event once before despawn so other systems can reward/cleanup.
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

            // 10% stats increase level-up 
            ServerApplyLevelUp10Percent();

            XPToNext.Value = CalcXpToNext(Level.Value);
        }
    }

    private int CalcXpToNext(int currentLevel)
    {
        float baseVal = Mathf.Max(1, xpToNextDefault);
        float scaled = baseVal * Mathf.Pow(1.15f, Mathf.Max(0, currentLevel - 1));
        return Mathf.Max(1, Mathf.CeilToInt(scaled));
    }

    private StatsProfile ResolveProfile()
    {
        // Monsters (explicit)
        if (GetComponent<Slime>() != null)
            return StatsProfile.Slime;

        // Player classes
        var ident = GetComponent<ClassIdentity>();
        if (ident != null)
        {
            return ident.classType switch
            {
                ClassType.Archer => StatsProfile.Archer,
                ClassType.Knight => StatsProfile.Knight,
                ClassType.Mage => StatsProfile.Mage,
                ClassType.Healer => StatsProfile.Healer,
                _ => StatsProfile.Generic
            };
        }

        return StatsProfile.Generic;
    }


}
