using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float maxJumpTime = 0.3f;
    public float jumpCancelMultiplier = 0.5f;
    
    private Rigidbody2D rb;
    private bool isGrounded = true;
    private bool isJumping = false;
    private float jumpTimeCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Horizontal Movement only (A and D keys)
        float moveX = Input.GetAxisRaw("Horizontal");
        
        Vector2 movement = new Vector2(moveX, 0).normalized;
        transform.Translate(movement * moveSpeed * Time.deltaTime);

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
}