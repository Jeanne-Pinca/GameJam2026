using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float maxJumpTime = 0.3f;
    public float jumpCancelMultiplier = 0.5f;
    [SerializeField] private RuntimeAnimatorController defaultAnimatorController;
    [SerializeField] private RuntimeAnimatorController maskedAnimatorController;
    
    private Rigidbody2D rb;
    private Animator animator;
    private TimeSwitch timeSwitch;
    private bool lastPastMode = false;
    private bool isGrounded = true;
    private bool isJumping = false;
    private float jumpTimeCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        timeSwitch = FindObjectOfType<TimeSwitch>();
        
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

    void Update()
    {
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