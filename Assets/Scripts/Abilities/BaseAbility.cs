using UnityEngine;

public abstract class BaseAbility : MonoBehaviour
{
    // The name we will use to find this ability (e.g., "Dash")
    public string abilityName;

    // This tracks when the ability will be ready again
    protected float readyTime = 0f;

    // Every ability must implement this function
    public abstract void Activate(GameObject user, int level);
}