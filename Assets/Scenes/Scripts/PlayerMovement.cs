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
        
        // Don't move if planting animation is playing
        if (IsPlantingAnimationPlaying())
        {
            return;
        }
        
        // Horizontal Movement only (A and D keys)
        float moveX = Input.GetAxisRaw("Horizontal");
        
        Vector2 movement = new Vector2(moveX, 0).normalized;
        transform.Translate(movement * moveSpeed * Time.deltaTime);

        // Update animator parameters
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(moveX));
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

        // Space to Jump - only when grounded
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

        // Plant animation trigger (Q key)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Only allow planting in past mode
            TimeSwitch timeSwitch = FindObjectOfType<TimeSwitch>();
            if (timeSwitch != null && timeSwitch.isPastMode && animator != null)
            {
                animator.SetTrigger("Plant");
            }
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // Player is grounded when touching any surface
        isGrounded = true;
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        // Player is no longer grounded when leaving a surface
        isGrounded = false;
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

    bool IsPlantingAnimationPlaying()
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("PlayerPlant") || stateInfo.IsName("PlayerPlantMasked");
    }
}