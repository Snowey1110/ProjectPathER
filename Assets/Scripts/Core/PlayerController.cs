using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] public stats stats; // Your existing stats script
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] public Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Settings")]
    public float moveSpeed = 5f;

    // --- Input System Variables ---
    private PlayerControls controls;
    private Vector2 moveInput;
    private bool inputActive = true;

    // --- Ability System ---
    // Dictionary to find abilities by string name ("Dash", "Heal", etc.)
    private Dictionary<string, BaseAbility> abilityMap = new Dictionary<string, BaseAbility>();

    public override void OnNetworkSpawn()
    {
        // If this is NOT my player, disable script so I don't control it
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        // Initialize Components
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Setup Systems
        InitializeAbilities();
        SetupInput();

        // Attach Camera (Your existing logic)
        GameObject cam = GameObject.FindWithTag("MainCamera");
        if (cam != null)
        {
            cam.GetComponent<CameraController>().player = this.gameObject;
        }
    }

    private void SetupInput()
    {
        controls = new PlayerControls();

        // Movement (WASD)
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Ability 1 (Mapped to 'E' in your Input Actions) -> Calls "Dash"
        controls.Player.Ability1.performed += ctx => UseAbility("Dash", 1);

        if (inputActive) controls.Player.Enable();
    }

    private void InitializeAbilities()
    {
        // Finds all ability scripts (DashAbility, etc.) attached to this object
        BaseAbility[] abilities = GetComponents<BaseAbility>();

        foreach (var ability in abilities)
        {
            if (!abilityMap.ContainsKey(ability.abilityName))
            {
                abilityMap.Add(ability.abilityName, ability);
                // Debug.Log($"Ability Registered: {ability.abilityName}");
            }
        }
    }

    // --- THE BRAIN: CALLING ABILITIES ---
    public void UseAbility(string name, int level)
    {
        if (!inputActive) return;

        // Check if we have this ability attached
        if (abilityMap.TryGetValue(name, out BaseAbility ability))
        {
            ability.Activate(this.gameObject, level);
        }
        else
        {
            Debug.LogWarning($"Ability '{name}' not found on Player!");
        }
    }

    // --- MAIN LOOP ---
    void Update()
    {
        if (!IsOwner) return;

        // Handle Animations
        bool isWalking = moveInput.magnitude > 0;
        animator.SetBool("walking", isWalking);

        // Handle Rotation (Flipping)
        // Only flip if we are NOT attacking (prevents moonwalking while shooting)
        if (!animator.GetBool("attacking"))
        {
            HandleRotation();
        }
    }

    void FixedUpdate()
    {
        // Physics movement belongs in FixedUpdate for smooth networking
        if (IsOwner && inputActive)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    void HandleRotation()
    {
        // Use Mouse Position for aiming direction
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (mousePos.x < transform.position.x)
        {
            spriteRenderer.flipX = true; // Face Left
        }
        else
        {
            spriteRenderer.flipX = false; // Face Right
        }
    }

    // --- UTILITIES ---
    public void SetInputActive(bool active)
    {
        inputActive = active;

        if (controls == null) return;

        if (active)
        {
            controls.Player.Enable();
        }
        else
        {
            controls.Player.Disable();
            // Stop moving immediately when menu opens
            if (rb != null) rb.linearVelocity = Vector2.zero;
            moveInput = Vector2.zero;
            animator.SetBool("walking", false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (controls != null) controls.Disable();
    }
}