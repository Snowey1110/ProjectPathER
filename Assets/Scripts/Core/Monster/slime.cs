using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Slime : NetworkBehaviour
{
    [Header("AI Ranges")]
    [SerializeField] private float detectionRange = 50f;

    // Jump trigger can be farther than "attack ready" range.
    [SerializeField] private float jumpTriggerRange = 12f;     // start trying to attack (windup) within this
    [SerializeField] private float attackReadyRange = 8f;      // only actually start windup when this close

    [Header("Speeds")]
    [SerializeField] private float crawlSpeed = 2f;
    [SerializeField] private float jumpCrawlSpeed = 8f;

    [Header("Combat")]
    [SerializeField] private float baseKnockbackForceOnHitPlayer = 6f; // slime knocks itself back after damaging player
    [SerializeField] private float knockbackForceWhenHit = 7f;         // slime knocked back when player hits slime

    [Header("Retarget")]
    [SerializeField] private float retargetInterval = 0.5f;
    private float retargetTimer;

    [Header("Jump Attack Timing")]
    [SerializeField] private Vector2 jumpWindupSeconds = new Vector2(0f, 2f);   // wait 0..2s before StartJump()
    [SerializeField] private Vector2 jumpCooldownSeconds = new Vector2(2f, 5f); // cooldown after starting jump
    [SerializeField] private float jumpMaxSecondsFailsafe = 0.9f;               // clears "jumping" if animation event fails

    [Header("Orbit-Walk (not jumping, inside jumpTriggerRange)")]
    [Tooltip("Orbit radius around the player while waiting for cooldown / setting up attack.")]
    [SerializeField] private float orbitRadius = 7f; // should generally be <= attackReadyRange

    [Tooltip("How close we need to get to the orbit waypoint before picking a new one.")]
    [SerializeField] private float orbitWaypointReachDist = 0.35f;

    [Tooltip("How often to pick a new waypoint around the orbit ring (seconds).")]
    [SerializeField] private Vector2 orbitPickIntervalSeconds = new Vector2(0.5f, 1.1f);

    [Tooltip("How far around the circle each waypoint step is (degrees).")]
    [SerializeField] private Vector2 orbitStepDegrees = new Vector2(25f, 75f);

    [Tooltip("Switch orbit direction about every ~3 seconds.")]
    [SerializeField] private Vector2 orbitSwitchSeconds = new Vector2(2.5f, 3.5f);

    [Header("Motor Lock (so impulses aren't overwritten by rb.linearVelocity)")]
    [SerializeField] private float motorLockSecondsAfterImpulse = 0.18f;

    [Header("Damage")]
    [SerializeField] private float contactDamageCooldown = 0.25f; // rate while colliding during jump

    // ---------------------------
    // Kill Rewards 
    // ---------------------------

    [Header("Kill Rewards")]
    [SerializeField] private float rewardRadius = 8f;

    [SerializeField] private int xpPerKill = 5;

    [Tooltip("TEST: first slime death in the match levels up all players in rewardRadius once.")]
    [SerializeField] private bool firstSlimeKillLevelsUp = true;

    private static bool s_firstSlimeKillConsumed = false;

    private Transform targetPlayer;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Vector2 _desiredVelocity;

    // Jump control
    private bool jumpCD;
    private Coroutine _windupRoutine;
    private Coroutine _cooldownRoutine;
    private Coroutine _jumpFailsafeRoutine;

    // Impulse protection
    private float _motorLockUntil;

    // Damage throttle
    private float _nextDamageAllowedTime;

    // Orbit-walk state
    private Vector2 _orbitWaypoint;
    private float _nextOrbitPickTime;
    private int _orbitSign = 1; // +1 / -1
    private float _nextOrbitSwitchTime;

    // Cached stats for death hook
    private stats _myStats;

    public override void OnNetworkSpawn()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (!IsServer) return;

        _nextOrbitSwitchTime = Time.time + Random.Range(orbitSwitchSeconds.x, orbitSwitchSeconds.y);
        _nextOrbitPickTime = 0f;

        // NEW: subscribe to stats death hook so we can reward players before despawn.
        _myStats = GetComponent<stats>();
        if (_myStats != null)
            _myStats.OnDiedServer += OnSlimeDiedServer;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && _myStats != null)
            _myStats.OnDiedServer -= OnSlimeDiedServer;

        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        // Safety: unsubscribe even if despawn path differs
        if (_myStats != null)
            _myStats.OnDiedServer -= OnSlimeDiedServer;
    }

    private void Update()
    {
        if (!IsServer) return;

        // Retarget
        retargetTimer -= Time.deltaTime;
        if (targetPlayer == null || retargetTimer <= 0f)
        {
            retargetTimer = retargetInterval;
            RetargetClosestPlayer();
        }

        if (targetPlayer == null)
        {
            _desiredVelocity = Vector2.zero;
            CancelWindup();
            return;
        }

        // Face target (visual only)
        if (spriteRenderer != null)
        {
            float dx = targetPlayer.position.x - transform.position.x;
            if (dx > 0f) spriteRenderer.flipX = false;
            else if (dx < 0f) spriteRenderer.flipX = true;
        }

        // If motor-locked, do not steer (let impulses play out)
        if (Time.time < _motorLockUntil)
        {
            _desiredVelocity = Vector2.zero;
            return;
        }

        bool jumping = animator != null && animator.GetBool("jumping");
        float dist = Vector2.Distance(transform.position, targetPlayer.position);

        // Start windup only when close enough to "attackReadyRange"
        if (!jumping && !jumpCD && dist <= attackReadyRange)
        {
            if (_windupRoutine == null)
                _windupRoutine = StartCoroutine(JumpWindupThenStart());
        }
        else
        {
            // If we aren't ready anymore, cancel a pending windup.
            CancelWindup();
        }

        // Movement
        if (jumping)
        {
            // fast chase while jump anim is active
            if (dist <= detectionRange)
            {
                Vector2 dir = ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized;
                _desiredVelocity = dir * jumpCrawlSpeed;
            }
            else
            {
                _desiredVelocity = Vector2.zero;
            }

            return;
        }

        // Not jumping
        if (dist <= jumpTriggerRange)
        {
            // Inside jump trigger range: orbit-walk by picking waypoints on a ring
            UpdateOrbitWaypoint();

            Vector2 toWp = _orbitWaypoint - (Vector2)transform.position;
            if (toWp.sqrMagnitude < orbitWaypointReachDist * orbitWaypointReachDist && Time.time >= _nextOrbitPickTime)
            {
                PickNewOrbitWaypoint();
                toWp = _orbitWaypoint - (Vector2)transform.position;
            }

            // Walk toward the waypoint (not perfectly attached: waypoint is discrete and repicked)
            Vector2 dir = toWp.sqrMagnitude > 0.0001f ? toWp.normalized : Vector2.zero;
            _desiredVelocity = dir * crawlSpeed;
        }
        else if (dist <= detectionRange)
        {
            // Outside trigger range: chase normally to catch up
            Vector2 dir = ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized;
            _desiredVelocity = dir * crawlSpeed;
        }
        else
        {
            _desiredVelocity = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        if (rb == null) return;

        // Do not overwrite knockback / impulses while locked
        if (Time.time < _motorLockUntil) return;

        rb.linearVelocity = _desiredVelocity;
    }

    // ---------------------------
    // Orbit-walk helpers
    // ---------------------------

    private void UpdateOrbitWaypoint()
    {
        // Switch direction about every ~3 seconds
        if (Time.time >= _nextOrbitSwitchTime)
        {
            _orbitSign *= -1;
            _nextOrbitSwitchTime = Time.time + Random.Range(orbitSwitchSeconds.x, orbitSwitchSeconds.y);
        }

        // If time to pick OR waypoint is invalid (e.g., first time), pick
        if (Time.time >= _nextOrbitPickTime || _orbitWaypoint == Vector2.zero)
        {
            PickNewOrbitWaypoint();
        }

        // If player moved far (slime drifted), also repick sooner so it doesn't "stick"
        // (This makes the behavior less attached than continuous orbit velocity.)
        float distToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
        if (distToPlayer > jumpTriggerRange + 1.0f)
        {
            _nextOrbitPickTime = Time.time; // repick immediately next Update
        }
    }

    private void PickNewOrbitWaypoint()
    {
        Vector2 playerPos = targetPlayer.position;
        Vector2 slimePos = transform.position;

        // Base angle from player->slime
        Vector2 fromPlayer = (slimePos - playerPos);
        float baseAngleDeg = Mathf.Atan2(fromPlayer.y, fromPlayer.x) * Mathf.Rad2Deg;

        // Step around the circle by some degrees in current orbit direction
        float stepDeg = Random.Range(orbitStepDegrees.x, orbitStepDegrees.y) * _orbitSign;
        float newAngleDeg = baseAngleDeg + stepDeg;
        float newAngleRad = newAngleDeg * Mathf.Deg2Rad;

        // Keep orbit radius inside attackReadyRange so jump can reach reliably
        float r = Mathf.Min(orbitRadius, attackReadyRange * 0.95f);

        _orbitWaypoint = playerPos + new Vector2(Mathf.Cos(newAngleRad), Mathf.Sin(newAngleRad)) * r;

        _nextOrbitPickTime = Time.time + Random.Range(orbitPickIntervalSeconds.x, orbitPickIntervalSeconds.y);
    }

    // ---------------------------
    // Jump control
    // ---------------------------

    private IEnumerator JumpWindupThenStart()
    {
        float wait = Random.Range(jumpWindupSeconds.x, jumpWindupSeconds.y);
        float end = Time.time + wait;

        while (Time.time < end)
        {
            if (targetPlayer == null) { _windupRoutine = null; yield break; }

            bool jumping = animator != null && animator.GetBool("jumping");
            float dist = Vector2.Distance(transform.position, targetPlayer.position);

            // Abort windup if were no longer ready
            if (jumping) { _windupRoutine = null; yield break; }
            if (jumpCD) { _windupRoutine = null; yield break; }
            if (dist > attackReadyRange) { _windupRoutine = null; yield break; }

            yield return null;
        }

        if (targetPlayer != null && !jumpCD && Vector2.Distance(transform.position, targetPlayer.position) <= attackReadyRange)
        {
            StartJump();
        }

        _windupRoutine = null;
    }

    private void CancelWindup()
    {
        if (_windupRoutine != null)
        {
            StopCoroutine(_windupRoutine);
            _windupRoutine = null;
        }
    }

    private void StartJump()
    {
        if (animator != null) animator.SetBool("jumping", true);

        jumpCD = true;

        if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = StartCoroutine(JumpCooldownRoutine());

        if (_jumpFailsafeRoutine != null) StopCoroutine(_jumpFailsafeRoutine);
        _jumpFailsafeRoutine = StartCoroutine(JumpFailsafeStopRoutine());
    }

    private IEnumerator JumpCooldownRoutine()
    {
        float cd = Random.Range(jumpCooldownSeconds.x, jumpCooldownSeconds.y);
        yield return new WaitForSeconds(cd);
        jumpCD = false;
        _cooldownRoutine = null;
    }

    private IEnumerator JumpFailsafeStopRoutine()
    {
        yield return new WaitForSeconds(jumpMaxSecondsFailsafe);

        // If animation event didn't clear it, clear anyway so it doesn't get stuck forever.
        if (animator != null && animator.GetBool("jumping"))
            animator.SetBool("jumping", false);

        _jumpFailsafeRoutine = null;
    }

    public void jumpingFalse()
    {
        if (!IsServer) return;
        if (animator != null) animator.SetBool("jumping", false);
    }

    // ---------------------------
    // Targeting
    // ---------------------------

    private void RetargetClosestPlayer()
    {
        targetPlayer = null;

        var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        float bestDistSq = float.MaxValue;
        Vector2 myPos = transform.position;

        foreach (var pc in players)
        {
            if (pc == null) continue;
            if (!pc.IsSpawned) continue;

            // Ignore lobby ghost
            if (pc.Class.Value == PlayerController.PlayerClass.Ghost) continue;

            // Ignore dead
            var st = pc.GetComponent<stats>();
            if (st != null && st.CurrentHP.Value <= 0) continue;

            float dSq = ((Vector2)pc.transform.position - myPos).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                targetPlayer = pc.transform;
            }
        }
    }

    // ---------------------------
    // Damage + knockback
    // ---------------------------

    // Damage should not rely on Enter (only fires once). Stay lets it apply again after cooldown.
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsServer) return;

        var pc = collision.gameObject.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        bool jumping = animator != null && animator.GetBool("jumping");
        if (!jumping) return;

        if (Time.time < _nextDamageAllowedTime) return;
        _nextDamageAllowedTime = Time.time + contactDamageCooldown;

        stats playerStats = pc.GetComponent<stats>();
        stats slimeStats = GetComponent<stats>();

        if (playerStats != null && slimeStats != null)
            playerStats.takeDamage(slimeStats.Damage.Value);

        // Knockback slime away from player after a successful hit
        ApplyImpulseAwayFrom(pc.transform.position, baseKnockbackForceOnHitPlayer);
    }

    // Call this when the player hits the slime (melee/projectile). Must run on server to replicate.
    // Recommended usage: from your server-side damage code, call slime.OnHitBy(attackerPosition).
    public void OnHitBy(Vector2 attackerPosition)
    {
        if (!IsServer)
        {
            // If you accidentally call this on client, forward to server.
            OnHitByServerRpc(attackerPosition);
            return;
        }

        ApplyImpulseAwayFrom(attackerPosition, knockbackForceWhenHit);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void OnHitByServerRpc(Vector2 attackerPosition)
    {
        ApplyImpulseAwayFrom(attackerPosition, knockbackForceWhenHit);
    }

    private void ApplyImpulseAwayFrom(Vector2 sourcePosition, float force)
    {
        if (rb == null) return;

        // Lock motor so our FixedUpdate doesn't immediately overwrite the impulse.
        _motorLockUntil = Time.time + motorLockSecondsAfterImpulse;

        Vector2 away = ((Vector2)transform.position - sourcePosition).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(away * force, ForceMode2D.Impulse);
    }

    // ---------------------------
    // Kill reward logic (server-side)
    // ---------------------------

    private void OnSlimeDiedServer(stats deadStats)
    {
        if (!IsServer) return;

        RewardPlayersInArea();

        // Consume the "first kill" test after we reward once.
        if (firstSlimeKillLevelsUp && !s_firstSlimeKillConsumed)
            s_firstSlimeKillConsumed = true;
    }

    private void RewardPlayersInArea()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rewardRadius);

        // Prevent double reward if multiple colliders belong to the same player object
        HashSet<ulong> rewardedOwners = new HashSet<ulong>();

        foreach (var h in hits)
        {
            if (h == null) continue;

            var pc = h.GetComponentInParent<PlayerController>();
            if (pc == null) continue;
            if (!pc.IsSpawned) continue;

            // Ignore lobby ghost
            if (pc.Class.Value == PlayerController.PlayerClass.Ghost) continue;

            var st = h.GetComponentInParent<stats>();
            if (st == null) continue;

            // Ignore dead (optional, but usually desired)
            if (st.CurrentHP.Value <= 0) continue;

            var no = st.GetComponent<NetworkObject>();
            if (no == null || !no.IsSpawned) continue;

            ulong ownerId = no.OwnerClientId;
            if (!rewardedOwners.Add(ownerId)) continue;

            // TEST: first slime death levels up everyone in range once
            if (firstSlimeKillLevelsUp && !s_firstSlimeKillConsumed)
            {
                // Data-driven stats refactor: use the unified level-up application.
                st.ServerApplyLevelUp();
            }
            else
            {
                // Requires stats.ServerAddXp(...) to exist (from the XP patch)
                st.ServerAddXp(xpPerKill);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, rewardRadius);
    }
}
