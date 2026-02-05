using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float maxJumpTime = 0.3f;
    public float jumpCancelMultiplier = 0.5f;
    [SerializeField] private RuntimeAnimatorController defaultAnimatorController;
    [SerializeField] private RuntimeAnimatorController maskedAnimatorController;
    
    // Debug teleport
    [SerializeField] private Vector2 debugTeleportPosition = new Vector2(45f, 3f);
    private float teleportHoldTime = 0f;
    private const float TELEPORT_HOLD_DURATION = 2f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private TimeSwitch timeSwitch;
    private ProceduralGeneration procGen;
    private bool lastPastMode = false;
    private bool isGrounded = true;
    private bool isJumping = false;
    private float jumpTimeCounter;
    private Vector3 startPosition; // Store initial position for reset

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        timeSwitch = FindObjectOfType<TimeSwitch>();
        procGen = FindObjectOfType<ProceduralGeneration>();
        startPosition = transform.position; // Save the starting position
        
        // Create and apply a physics material with zero friction for smooth wall sliding
        PhysicsMaterial2D playerMaterial = new PhysicsMaterial2D();
        playerMaterial.friction = 0f;
        playerMaterial.bounciness = 0f;
        
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCollider.sharedMaterial = playerMaterial;
        }
    }

    public void ResetPlayer()
    {
        // Reset position to starting location
        transform.position = startPosition;
        
        // Reset velocity
        rb.velocity = Vector2.zero;
        
        // Reset jump state
        isJumping = false;
        isGrounded = false;
        jumpTimeCounter = 0f;
        teleportHoldTime = 0f;
        
        // Reset animator
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        
        // CRITICAL: Also reset camera position to player position so terrain generates from the right location
        if (procGen != null && procGen.GetTargetCamera() != null)
        {
            Transform cam = procGen.GetTargetCamera();
            cam.position = new Vector3(startPosition.x, startPosition.y, cam.position.z);
        }
        
        Debug.Log("Player reset to starting position: " + startPosition);
    }

    void Update()
    {
        // Debug teleport: Hold Z for 3 seconds
        if (Input.GetKey(KeyCode.Z))
        {
            teleportHoldTime += Time.deltaTime;
            if (teleportHoldTime >= TELEPORT_HOLD_DURATION)
            {
                transform.position = debugTeleportPosition;
                rb.velocity = Vector2.zero;
                isGrounded = false; // Let physics detect ground naturally after falling
                transform.localScale = new Vector3(1, 1, 1); // Face right after teleport
                teleportHoldTime = 0f;
                
                // Regenerate terrain properly at the new location
                if (procGen != null)
                {
                    procGen.RegenerateTerrainAtLocation();
                }
                
                Debug.Log($"Teleported to {debugTeleportPosition}");
            }
        }
        else
        {
            teleportHoldTime = 0f;
        }
        
        // Check if mode switched and update animator
        if (timeSwitch != null && timeSwitch.isPastMode != lastPastMode)
        {
            UpdateAnimatorController(timeSwitch.isPastMode);
            lastPastMode = timeSwitch.isPastMode;
        }
        
        // Don't move if a plant is growing (player is being held on the plant)
        if (PlantGrowthAnimator.IsPlantGrowing)
        {
            return;
        }
        
        // Don't do any action if planting animation is playing
        if (IsPlantingAnimationPlaying())
        {
            return;
        }
        
        // Space to Jump - only when grounded (process BEFORE animator update for immediate response)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isJumping = true;
            jumpTimeCounter = maxJumpTime;
            isGrounded = false;
        }
        
        // Continue jumping while holding space
        if (Input.GetKey(KeyCode.Space) && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
        
        // Release space to stop jumping early
        if (Input.GetKeyUp(KeyCode.Space))
        {
            isJumping = false;
            if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCancelMultiplier);
            }
        }
        
        // Horizontal Movement only (A and D keys)
        float moveX = Input.GetAxisRaw("Horizontal");
        
        // Use Rigidbody velocity for smooth physics-based movement
        rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);

        // Update animator parameters AFTER processing jump input
        if (animator != null)
        {
            animator.SetFloat("Speed", isGrounded ? Mathf.Abs(moveX) : 0f);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VelocityY", rb.velocity.y);
        }

        // Flip sprite based on direction
        if (moveX > 0)
        {
            transform.localScale = new Vector3(1, 1, 1); // Face right
        }
        else if (moveX < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1); // Face left
        }

        // Plant animation trigger (Q key)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Only allow planting in past mode and when grounded
            TimeSwitch timeSwitch = FindObjectOfType<TimeSwitch>();
            if (timeSwitch != null && timeSwitch.isPastMode && animator != null && isGrounded)
            {
                // Reset animator to immediately stop any current animation
                animator.Rebind();
                animator.Update(0f);
                animator.SetTrigger("Plant");
            }
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // Only set grounded if the collision is below the player (check contact normals)
        // AND the player is not moving upward (prevents being grounded mid-jump)
        if (rb.velocity.y <= 0.1f)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // If the normal is pointing upward (y > 0.5), player is standing on top of something
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        // Check if we're actually leaving the ground (not just a wall)
        // Set to false only if we have upward or significant velocity
        if (rb.velocity.y > 0.1f || !IsStandingOnGround())
        {
            isGrounded = false;
        }
    }
    
    bool IsStandingOnGround()
    {
        // Raycast downward to check if truly on ground
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.6f);
        return hit.collider != null;
    }

    void UpdateAnimatorController(bool isPastMode)
    {
        if (animator != null)
        {
            // Save current grounded state
            bool wasGrounded = isGrounded;
            
            if (isPastMode && maskedAnimatorController != null)
            {
                animator.runtimeAnimatorController = maskedAnimatorController;
            }
            else if (!isPastMode && defaultAnimatorController != null)
            {
                animator.runtimeAnimatorController = defaultAnimatorController;
            }
            
            // Reset animator to avoid glitching
            animator.Rebind();
            animator.Update(0f);
            
            // Restore grounded state
            isGrounded = wasGrounded;
        }
    }

    public bool IsPlantingAnimationPlaying()
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("PlayerPlant") || stateInfo.IsName("PlayerPlantMasked");
    }
}