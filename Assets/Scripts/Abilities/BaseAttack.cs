using UnityEngine;
using Unity.Netcode;

public abstract class BaseAttack : NetworkBehaviour
{
    // Every weapon needs a cooldown (fire rate)
    public float attackRate = 0.5f;
    protected float nextAttackTime = 0f;

    // The Controller calls this when you click
    public abstract void Fire(Vector2 direction);
}