using Unity.Netcode;
using UnityEngine;

public class stats : NetworkBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Base Stats")]
    public int HP = 10;
    public int defense;
    public int mana;
    public int baseDamage;
    public int level;
    public int abilityPoints;
    public int skillPoints = 1;

    // Networked HP (server writes, everyone reads)
    public NetworkVariable<int> MaxHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentHP = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            MaxHP.Value = Mathf.Max(1, HP);
            CurrentHP.Value = MaxHP.Value;
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
    // Server-authoritative API
    // ----------------------------

    /// Server-only. Apply damage to this entity.
    /// Existing callers can keep using takeDamage(), but it must run on the server.
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

    /// Server-only heal.
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

    /// Server-only setter if you need to apply loadouts/saves.
    public void ServerSetMaxHpAndFill(int newMaxHp)
    {
        if (!IsServer) return;

        MaxHP.Value = Mathf.Max(1, newMaxHp);
        CurrentHP.Value = MaxHP.Value;
    }

    private void DieServer()
    {
        if (!IsServer) return;

        // Despawn networked objects on server
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
