using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Lightweight owner-driven animator helper.
///
/// This project previously used Netcode's NetworkAnimator. Some Netcode versions or assembly
/// configurations may not include that type, so this class avoids any dependency on it.
///
/// Current gameplay/visual logic uses explicit RPCs for remote animation triggers (see ArcherAttack).
/// This helper remains as a safe drop-in component if other scripts reference it in the future.
/// </summary>
public class ClientNetworkAnimator : NetworkBehaviour
{
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void SetTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return;
        animator?.SetTrigger(triggerName);
    }

    public void SetFloat(string paramName, float value)
    {
        if (string.IsNullOrEmpty(paramName)) return;
        animator?.SetFloat(paramName, value);
    }
}
