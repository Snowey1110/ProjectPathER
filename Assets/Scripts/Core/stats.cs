using Unity.Netcode;
using UnityEngine;

public class stats : MonoBehaviour
{
    public HealthBar healthBar;

    public int HP;
    public int currentHealth;
    public int defense;
    public int mana;
    public int baseDamage;
    public int level;
    public int abilityPoints;
    public int skillPoints = 1;

    private NetworkObject _netObj;

    private void Awake()
    {
        _netObj = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        currentHealth = HP;
        if (healthBar != null)
            healthBar.SetMaxHealth(HP, currentHealth);
    }

    // Keep the same method name so your existing calls still compile.
    public void takeDamage(int damageReceived)
    {
        currentHealth -= damageReceived;

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        // If this is a network-spawned object, the SERVER must despawn it.
        if (_netObj != null && _netObj.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            _netObj.Despawn(true);
            return;
        }

        // Fallback for non-network objects or client-only cases
        Destroy(gameObject);
    }
}
