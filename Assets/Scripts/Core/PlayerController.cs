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

    // Server-authoritative replicated settings
    public NetworkVariable<bool> FriendlyFire = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<PlayerClass> Class = new NetworkVariable<PlayerClass>(
        PlayerClass.Ghost,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

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
            var ident = GetComponent<ClassIdentity>(); // the small script that holds ClassType
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

        // Always locate the camera under this prefab (if any)
        _localCamera = GetComponentInChildren<Camera>(true);

        if (!IsOwner)
        {
            // Disable any camera/audio on non-owned player instances
            DisableLocalOnlyComponentsForRemote();
            enabled = false;
            return;
        }

        EnableLocalOnlyComponentsForOwner();

        primaryAttack = GetComponent<BaseAttack>();

        InitializeAbilities();
        SetupInput();
    }

    private void DisableLocalOnlyComponentsForRemote()
    {
        // Disable any camera(s) in this prefab instance
        foreach (var cam in GetComponentsInChildren<Camera>(true))
        {
            cam.enabled = false;
            if (cam.CompareTag("MainCamera"))
                cam.tag = "Untagged";
        }

        // Disable AudioListener if present
        foreach (var al in GetComponentsInChildren<AudioListener>(true))
            al.enabled = false;

        // Disable the camera controller script so it doesn’t move the camera
        var camController = GetComponentInChildren<CameraController>(true);
        if (camController != null) camController.enabled = false;
    }

    private void EnableLocalOnlyComponentsForOwner()
    {
        // Ensure ONLY the owning player's camera becomes MainCamera
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
        if (primaryAttack == null) return;

        // Use the owner's camera, not Camera.main
        if (_localCamera == null) return;

        Vector2 mousePos = _localCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction = (mousePos - (Vector2)transform.position).normalized;

        primaryAttack.Fire(direction);

        if (animator != null) animator.SetTrigger("attack");
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
        if (!inputActive) return;

        if (abilityMap.TryGetValue(name, out BaseAbility ability))
            ability.Activate(this.gameObject, level);
        else
            Debug.LogWarning($"Ability '{name}' not found on Player!");
    }

    void Update()
    {
        if (!IsOwner) return;

        bool isWalking = moveInput.magnitude > 0;
        if (animator != null) animator.SetBool("walking", isWalking);

        if (animator != null && !animator.GetBool("attack"))
            HandleRotation();
    }

    void FixedUpdate()
    {
        if (IsOwner && inputActive && rb != null)
            rb.linearVelocity = moveInput * moveSpeed;
    }

    void HandleRotation()
    {
        if (_localCamera == null || spriteRenderer == null) return;

        Vector2 mousePos = _localCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        spriteRenderer.flipX = mousePos.x < transform.position.x;
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
    }

    // TEST: Server RPC to set friendly fire
    [ServerRpc(RequireOwnership = true)]
    public void SetFriendlyFireServerRpc(bool v)
    {
        FriendlyFire.Value = v;
    }
}
