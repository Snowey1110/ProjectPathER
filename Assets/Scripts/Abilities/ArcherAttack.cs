using Unity.Netcode.Components;
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
    private ClientNetworkAnimator m_bodyNetAnimator;
    private Animator m_bowAnimator;
    private NetworkAnimator m_bowNetAnimator;
    private float m_bowShootClipLen = -1f;

    private void Awake()
    {
        // Body animator is on the root
        if (m_bodyNetAnimator == null)
            m_bodyNetAnimator = GetComponent<ClientNetworkAnimator>();

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

        if (m_bowNetAnimator == null)
        {
            var netAnims = GetComponentsInChildren<NetworkAnimator>(true);
            foreach (var na in netAnims)
            {
                if (na != null && na.Animator != null && na.Animator.runtimeAnimatorController != null && na.Animator.runtimeAnimatorController.name == "Bow")
                {
                    m_bowNetAnimator = na;
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
        if (m_bodyNetAnimator != null)
            m_bodyNetAnimator.SetTrigger(bodyAttackTrigger);
        else
            GetComponent<Animator>()?.SetTrigger(bodyAttackTrigger);

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

        // Trigger bow animation (controller has AnyState->BowShoot on this trigger so it restarts from frame 0)
        if (m_bowNetAnimator != null)
            m_bowNetAnimator.SetTrigger(bowShootTrigger);
        else
            m_bowAnimator?.SetTrigger(bowShootTrigger);
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
            arrow.ServerInit(dir, arrowSpeed, dmg, shooterId, ff, enablePercentHpBonus);
    }
}
