using Unity.Netcode;
using UnityEngine;

public class ArcherAttack : BaseAttack
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 20f;
    [SerializeField] private float extraSpawnPadding = 0.20f;

    [Header("Animation (Archer-specific)")]
    [Tooltip("Body attack trigger on the character Animator/NetworkAnimator.")]
    [SerializeField] private string bodyAttackTrigger = "attack";

    [Tooltip("Bow shoot trigger on the Bow Animator/NetworkAnimator.")]
    [SerializeField] private string bowShootTrigger = "Shoot";

    [Tooltip("Bow shoot state/clip name (used to look up clip length).")]
    [SerializeField] private string bowShootClipName = "BowShoot";

    [Tooltip("Float parameter on Bow Animator that scales the BowShoot state's speed.")]
    [SerializeField] private string bowSpeedMultParam = "SpeedMult";

    [SerializeField] private float minSpeedMult = 0.05f;
    [SerializeField] private float maxSpeedMult = 20f;

    // If you ever reuse Arrow for Mage, you can toggle this off in a Mage-specific attack.
    [Header("Archer Bonus")]
    [Tooltip("When true, Arrow applies percent-of-current-HP bonus damage to non-player targets.")]
    [SerializeField] private bool enablePercentHpBonus = true;

    // Cached (auto-discovered to avoid coupling PlayerController to Archer)
    private Animator m_bodyAnimator;
    private Animator m_bowAnimator;
    private float m_bowShootClipLen = -1f;

    private void Awake()
    {
        // Body animator is on the root
        if (m_bodyAnimator == null)
            m_bodyAnimator = GetComponent<Animator>();

        // Bow animator + bow NetworkAnimator live on the Bow child object.
        if (m_bowAnimator == null)
        {
            var anims = GetComponentsInChildren<Animator>(true);
            foreach (var a in anims)
            {
                if (a != null && a.runtimeAnimatorController != null && a.runtimeAnimatorController.name == "Bow")
                {
                    m_bowAnimator = a;
                    break;
                }
            }
        }

        CacheBowShootClipLen();
    }

    public override bool TryFire(Vector2 direction)
    {
        if (!IsOwner) return false;

        // Cooldown gating.
        if (Time.time < nextAttackTime) return false;

        if (direction.sqrMagnitude < 0.0001f) return false;
        direction.Normalize();

        nextAttackTime = Time.time + attackRate;

        // Local visuals (and network-synced triggers via NetworkAnimator)
        PlayAttackVisuals();

        // Gameplay
        SpawnArrowRpc(direction, NetworkObjectId);
        return true;
    }

    private void CacheBowShootClipLen()
    {
        if (m_bowShootClipLen > 0f) return;
        if (m_bowAnimator == null) return;
        if (m_bowAnimator.runtimeAnimatorController == null) return;

        foreach (var clip in m_bowAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == bowShootClipName)
            {
                m_bowShootClipLen = Mathf.Max(0.0001f, clip.length);
                return;
            }
        }

        m_bowShootClipLen = -1f;
    }

    private void PlayAttackVisuals()
    {
        // Body attack
        m_bodyAnimator?.SetTrigger(bodyAttackTrigger);

        // Bow shoot + reload should match fire rate.
        if (m_bowAnimator != null)
        {
            CacheBowShootClipLen();

            if (m_bowShootClipLen > 0f && attackRate > 0.0001f)
            {
                float speedMult = m_bowShootClipLen / attackRate;
                speedMult = Mathf.Clamp(speedMult, minSpeedMult, maxSpeedMult);
                m_bowAnimator.SetFloat(bowSpeedMultParam, speedMult);
            }
        }

        // Trigger bow animation locally (controller has AnyState->BowShoot on this trigger so it restarts from frame 0)
        m_bowAnimator?.SetTrigger(bowShootTrigger);

        // Also notify other clients to play the same bow shot visuals (Steam/Netcode-safe: no NetworkAnimator dependency).
        if (IsOwner)
        {
            float speedMult = 1f;
            if (m_bowAnimator != null)
                speedMult = m_bowAnimator.GetFloat(bowSpeedMultParam);
            BowShotVisualsServerRpc(speedMult);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void BowShotVisualsServerRpc(float bowSpeedMult, RpcParams rpcParams = default)
    {
        // Broadcast to all clients (including host). Owner will early-out in the ClientRpc.
        BowShotVisualsClientRpc(bowSpeedMult);
    }

    [ClientRpc]
    private void BowShotVisualsClientRpc(float bowSpeedMult, ClientRpcParams clientRpcParams = default)
    {
        // Play on non-owner clients.
        if (IsOwner) return;

        if (m_bowAnimator == null)
            Awake(); // best-effort cache; safe to call once or twice

        if (m_bowAnimator != null)
        {
            m_bowAnimator.SetFloat(bowSpeedMultParam, bowSpeedMult);
            m_bowAnimator.SetTrigger(bowShootTrigger);
        }

        // Body attack animation for other clients (optional, but keeps visuals consistent).
        if (m_bodyAnimator == null)
            m_bodyAnimator = GetComponent<Animator>();
        m_bodyAnimator?.SetTrigger(bodyAttackTrigger);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnArrowRpc(Vector2 dir, ulong shooterId, RpcParams rpcParams = default)
    {
        if (arrowPrefab == null) return;

        // Find shooter object on server
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterId, out var shooterNo) ||
            shooterNo == null)
        {
            return;
        }

        // Validate ownership: only the owner of shooter can request shots
        if (shooterNo.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Vector3 shooterPos = shooterNo.transform.position;

        // Spawn outside shooter collider
        float radius = 0f;
        Collider2D shooterCol = shooterNo.GetComponentInChildren<Collider2D>();
        if (shooterCol != null) radius = shooterCol.bounds.extents.magnitude;

        Vector3 spawnPos = shooterPos + (Vector3)dir * (radius + extraSpawnPadding);

        // Compute rotation BEFORE Spawn() so clients receive correct orientation in spawn payload
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Read offset from prefab's Arrow component (so art alignment stays in one place)
        float offsetDeg = 0f;
        var prefabArrow = arrowPrefab.GetComponent<Arrow>();
        if (prefabArrow != null)
            offsetDeg = prefabArrow.SpriteAngleOffsetDeg;

        Quaternion spawnRot = Quaternion.Euler(0f, 0f, angle + offsetDeg);

        GameObject arrowGo = Instantiate(arrowPrefab, spawnPos, spawnRot);

        var arrowNo = arrowGo.GetComponent<NetworkObject>();
        if (arrowNo == null)
        {
            Destroy(arrowGo);
            return;
        }

        // Damage from shooter stats (base + flat bonus)
        int dmg = 1;
        var shooterStats = shooterNo.GetComponent<stats>();
        if (shooterStats != null)
            dmg = Mathf.Max(1, shooterStats.Damage.Value + shooterStats.BonusDamage.Value);

        // Friendly fire from PlayerController (server authoritative)
        bool ff = false;
        var shooterPC = shooterNo.GetComponent<PlayerController>();
        if (shooterPC != null) ff = shooterPC.FriendlyFire.Value;

        // Spawn on network (clients will create with spawnRot)
        arrowNo.Spawn(true);

        // Initialize server physics + collision ignore rules
        var arrow = arrowGo.GetComponent<Arrow>();
        if (arrow != null)
        {
            bool enablePct = enablePercentHpBonus;
            float pctNormal = 0.10f;
            float pctBoss = 0.03f;
            string bossTag = "Boss";

            if (shooterStats != null && shooterStats.Definition != null)
            {
                enablePct = shooterStats.Definition.enablePercentCurrentHpBonusOnHit;
                pctNormal = shooterStats.Definition.percentCurrentHpBonus;
                pctBoss = shooterStats.Definition.percentCurrentHpBonusBoss;
                bossTag = shooterStats.Definition.bossTag;
            }

            arrow.ServerInit(dir, arrowSpeed, dmg, shooterId, ff, enablePct, pctNormal, pctBoss, bossTag);
        }
    }
}
