using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public enum PlayerClass
    {
        Ghost = 0,
        Archer = 1,
        Knight = 2,
        Mage = 3,
        Healer = 4
    }

    [Header("Combat")]
    private BaseAttack primaryAttack;

    [Header("Combat Rules")]
    [SerializeField] private bool defaultFriendlyFire = true;

    [Header("Walk Animation Speed")]
    [SerializeField] private string walkSpeedParam = "WalkSpeedMult";
    private const float WALK_SPEED_DIVISOR = 8.0f;

    [Header("Bow Animation")]
    [SerializeField] private Unity.Netcode.Components.NetworkAnimator bowNetAnimator;
    [SerializeField] private string bowShootTrigger = "Shoot";




    // Server-authoritative replicated settings
    public NetworkVariable<bool> FriendlyFire = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<PlayerClass> Class = new NetworkVariable<PlayerClass>(
        PlayerClass.Ghost,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // NEW: replicated facing (true = facing left / flipX on)
    public NetworkVariable<bool> FacingLeft = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Facing Sync")]
    [SerializeField] private float facingSendRateHz = 20f;
    private float _facingSendTimer;
    private bool _lastSentFacingLeft;

    [Header("References")]
    [SerializeField] public stats stats;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] public Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Settings")]
    public float moveSpeed = 5f;

    private PlayerControls controls;
    private Vector2 moveInput;
    private bool inputActive = true;

    private Dictionary<string, BaseAbility> abilityMap = new Dictionary<string, BaseAbility>();

    // Cache the local camera for this owned player (do NOT rely on Camera.main)
    private Camera _localCamera;

    public override void OnNetworkSpawn()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (IsServer)
        {
            // Default friendly fire for this player
            FriendlyFire.Value = defaultFriendlyFire;

            // Determine class from ClassIdentity if present (Archer/Knight/etc). Otherwise Ghost.
            PlayerClass resolved = PlayerClass.Ghost;
            var ident = GetComponent<ClassIdentity>();
            if (ident != null)
            {
                resolved = ident.classType switch
                {
                    ClassType.Archer => PlayerClass.Archer,
                    ClassType.Knight => PlayerClass.Knight,
                    ClassType.Mage => PlayerClass.Mage,
                    ClassType.Healer => PlayerClass.Healer,
                    _ => PlayerClass.Ghost
                };
            }
            Class.Value = resolved;
        }

        // Subscribe so remote clients update visuals when FacingLeft changes
        FacingLeft.OnValueChanged += OnFacingChanged;
        ApplyFacing(FacingLeft.Value);

        // Always locate the camera under this prefab (if any)
        _localCamera = GetComponentInChildren<Camera>(true);

        if (!IsOwner)
        {
            // Disable any camera/audio on non-owned player instances
            DisableLocalOnlyComponentsForRemote();

            // IMPORTANT: keep this script enabled so it can apply FacingLeft.Value
            // Just prevent input/movement/attack by gating with IsOwner elsewhere.
            inputActive = false;
            return;
        }

        EnableLocalOnlyComponentsForOwner();

        primaryAttack = GetComponent<BaseAttack>();

        InitializeAbilities();
        SetupInput();
    }

    private void OnFacingChanged(bool oldV, bool newV)
    {
        ApplyFacing(newV);
    }

    private void ApplyFacing(bool facingLeft)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingLeft;
    }

    private void DisableLocalOnlyComponentsForRemote()
    {
        foreach (var cam in GetComponentsInChildren<Camera>(true))
        {
            cam.enabled = false;
            if (cam.CompareTag("MainCamera"))
                cam.tag = "Untagged";
        }

        foreach (var al in GetComponentsInChildren<AudioListener>(true))
            al.enabled = false;

        var camController = GetComponentInChildren<CameraController>(true);
        if (camController != null) camController.enabled = false;
    }

    private void EnableLocalOnlyComponentsForOwner()
    {
        if (_localCamera != null)
        {
            _localCamera.enabled = true;
            _localCamera.tag = "MainCamera";

            var camController = _localCamera.GetComponent<CameraController>();
            if (camController != null)
            {
                camController.enabled = true;
                camController.player = transform;
            }
        }
    }

    private void SetupInput()
    {
        controls = new PlayerControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += _ => moveInput = Vector2.zero;

        controls.Player.Ability1.performed += _ => UseAbility("Dash", 1);
        controls.Player.Attack.performed += _ => PerformAttack();

        if (inputActive) controls.Player.Enable();
    }

    private void PerformAttack()
    {
        if (!IsOwner) return;
        if (primaryAttack == null) return;
        if (_localCamera == null) return;

        Vector2 mousePos = _localCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction = (mousePos - (Vector2)transform.position).normalized;

        primaryAttack.Fire(direction);

        if (animator != null) animator.SetTrigger("attack");

        if (bowNetAnimator != null)
            bowNetAnimator.SetTrigger(bowShootTrigger);

    }

    private void InitializeAbilities()
    {
        BaseAbility[] abilities = GetComponents<BaseAbility>();
        foreach (var ability in abilities)
        {
            if (!abilityMap.ContainsKey(ability.abilityName))
                abilityMap.Add(ability.abilityName, ability);
        }
    }

    public void UseAbility(string name, int level)
    {
        if (!IsOwner) return;
        if (!inputActive) return;

        if (abilityMap.TryGetValue(name, out BaseAbility ability))
            ability.Activate(this.gameObject, level);
        else
            Debug.LogWarning($"Ability '{name}' not found on Player!");
    }

    void Update()
    {
        // Owners drive input + animation params
        if (IsOwner)
        {
            if (animator != null)
            {
                bool isWalking = moveInput.sqrMagnitude > 0.0001f;
                animator.SetBool("walking", isWalking);

                // Always recompute each frame so inspector changes take effect immediately
                UpdateWalkAnimSpeed(isWalking);
            }

            HandleRotationOwnerAndSyncFacing();
        }
        else
        {
            ApplyFacing(FacingLeft.Value);
        }


    }

    void FixedUpdate()
    {
        if (IsOwner && inputActive && rb != null)
            rb.linearVelocity = moveInput * moveSpeed;
    }

    // Owner computes facing from mouse and syncs to server
    void HandleRotationOwnerAndSyncFacing()
    {
        if (_localCamera == null || spriteRenderer == null) return;

        Vector2 mousePos = _localCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bool facingLeft = mousePos.x < transform.position.x;

        // Apply locally immediately
        spriteRenderer.flipX = facingLeft;

        // Rate-limit + only send when changed
        _facingSendTimer -= Time.deltaTime;
        if (facingLeft != _lastSentFacingLeft && _facingSendTimer <= 0f)
        {
            _facingSendTimer = 1f / Mathf.Max(1f, facingSendRateHz);
            _lastSentFacingLeft = facingLeft;
            SetFacingLeftServerRpc(facingLeft);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SetFacingLeftServerRpc(bool facingLeft)
    {
        FacingLeft.Value = facingLeft;
    }

    public void SetInputActive(bool active)
    {
        inputActive = active;

        if (controls == null) return;

        if (active)
            controls.Player.Enable();
        else
        {
            controls.Player.Disable();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            moveInput = Vector2.zero;
            if (animator != null) animator.SetBool("walking", false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (controls != null) controls.Disable();
        FacingLeft.OnValueChanged -= OnFacingChanged;
    }

    private void UpdateWalkAnimSpeed(bool isWalking)
    {
        if (animator == null) return;

        float walkMult = isWalking ? (moveSpeed / WALK_SPEED_DIVISOR) : 1f;
        animator.SetFloat(walkSpeedParam, walkMult);
    }

    // Called in editor when values change in the Inspector.
    // During Play Mode, update the animator immediately.
    private void OnValidate()
    {

        if (!Application.isPlaying) return;
        // Only the owner should drive animation params
        if (!IsOwner) return;

        bool isWalking = moveInput.sqrMagnitude > 0.0001f;
        UpdateWalkAnimSpeed(isWalking);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SetFriendlyFireServerRpc(bool v)
    {
        FriendlyFire.Value = v;
    }


}
