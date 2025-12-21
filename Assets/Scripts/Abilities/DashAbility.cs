using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class DashAbility : BaseAbility
{
    public int dashLevel;
    private bool isDashing = false;
    private Rigidbody2D rb;

    private void Awake()
    {
        abilityName = "Dash";
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Activate(GameObject user, int level)
    {
        // COOLDOWN CHECK
        // If the current time is less than our ready time, STOP.
        if (Time.time < readyTime || isDashing) return;

        // DEFINE STATS BASED ON LEVEL (Restoring your original logic)
        float cooldown = 0f;
        float dashDistance = 0f;
        float dashSpeed = 0f;

        if (level == 1) { cooldown = 10f; dashDistance = 10f; dashSpeed = 30f; }
        else if (level == 2) { cooldown = 5f; dashDistance = 5f; dashSpeed = 30f; }
        else if (level == 3) { cooldown = 3f; dashDistance = 3f; dashSpeed = 35f; }
        else if (level == 4) { cooldown = 1.5f; dashDistance = 1.5f; dashSpeed = 40f; }
        else if (level >= 5) { cooldown = 1f; dashDistance = 1f; dashSpeed = 40f; }

        // APPLY COOLDOWN
        readyTime = Time.time + cooldown;

        // EXECUTE
        StartCoroutine(PerformDash(dashDistance, dashSpeed));
    }

    private IEnumerator PerformDash(float distance, float speed)
    {
        isDashing = true;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction = (mousePos - (Vector2)transform.position).normalized;

        float duration = distance / speed;
        float startTime = Time.time;

        while (Time.time < startTime + duration)
        {
            rb.linearVelocity = direction * speed;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }
}