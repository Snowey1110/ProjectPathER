using UnityEngine;
using Unity.Netcode;

public abstract class BaseAttack : NetworkBehaviour
{
    // Every weapon needs a cooldown (fire rate)
    public float attackRate = 0.5f;
    protected float nextAttackTime = 0f;

    // The Controller calls this when you click.
    // Return true only when the attack actually fired (i.e., cooldown passed and input was valid).
    public abstract bool TryFire(Vector2 direction);
}