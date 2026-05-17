using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  REFERENCES
    // ─────────────────────────────────────────────
    [Header("References")]
    [SerializeField] Animator animator;
    [SerializeField] Transform playerObj;       // Visual mesh to rotate
    [SerializeField] Transform cameraTransform; // Main Camera

    CharacterController controller;

    // ─────────────────────────────────────────────
    //  MOVEMENT SETTINGS
    // ─────────────────────────────────────────────
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed          = 5f;
    [SerializeField] float sprintSpeed        = 10f;
    [SerializeField] float sprintTransitSpeed = 5f;
    [SerializeField] float airSpeed           = 3.5f;
    [SerializeField] float rotationSpeed      = 10f;
    float sprint;
    float idle = 0f;
    float speed;

    // ─────────────────────────────────────────────
    //  GRAVITY
    // ─────────────────────────────────────────────
    [Header("Gravity")]
    [SerializeField] float gravity = 20f;
    float verticalVelocity;

    // ─────────────────────────────────────────────
    //  JUMP SETTINGS
    // ─────────────────────────────────────────────
    [Header("Jump Settings")]
    [SerializeField] float jumpHeight   = 4f;    // Higher than before (was 2)
    [SerializeField] float jumpCooldown = 0.4f;  // Prevents the isGrounded-flicker double-jump bug
    bool  jumpRequested;
    float lastJumpTime = -999f;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    Vector3 moveDir;
    Vector3 inputMove;

    // ─────────────────────────────────────────────
    //  START
    // ─────────────────────────────────────────────
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator   = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    // ─────────────────────────────────────────────
    //  UPDATE — input reading
    // ─────────────────────────────────────────────
    void Update()
    {
        // Read WASD input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Camera-relative movement
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight   = cameraTransform.right;
            camForward.y = 0f;
            camRight.y   = 0f;
            camForward.Normalize();
            camRight.Normalize();

            inputMove = (camForward * v + camRight * h).normalized;
        }
        else
        {
            inputMove = new Vector3(h, 0f, v).normalized;
        }

        // Sprint
        sprint = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;

        // Jump input — only QUEUE the request here.
        // The actual jump fires in FixedUpdate so it can't double-trigger.
        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;

        // Rotate visual mesh toward movement direction
        if (inputMove != Vector3.zero && playerObj != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputMove);
            playerObj.rotation   = Quaternion.Slerp(playerObj.rotation,
                                                     targetRot,
                                                     rotationSpeed * Time.deltaTime);
        }
    }

    // ─────────────────────────────────────────────
    //  FIXED UPDATE — physics
    // ─────────────────────────────────────────────
    void FixedUpdate()
    {
        GroundMovement();
    }

    // ─────────────────────────────────────────────
    //  GROUND MOVEMENT
    // ─────────────────────────────────────────────
    void GroundMovement()
    {
        moveDir = inputMove;

        // "Stable grounded" = on the ground AND not in jump cooldown.
        // This ignores the one-frame isGrounded flicker that causes double jumps.
        bool stableGrounded = controller.isGrounded
                              && Time.time - lastJumpTime > jumpCooldown;

        // Animator states
        animator.SetBool("Grounded", stableGrounded);
        animator.SetBool("FreeFall", !stableGrounded && verticalVelocity < 0f);
        animator.SetBool("Jumping",  !stableGrounded && verticalVelocity > 0f);

        if (controller.isGrounded)
        {
            // Pick target speed: idle / walk / sprint
            float targetSpeed = (sprint > 0) ? sprintSpeed : walkSpeed;
            if (moveDir == Vector3.zero) targetSpeed = idle;

            speed = Mathf.Lerp(speed, targetSpeed, sprintTransitSpeed * Time.deltaTime);
            animator.SetFloat("Speed", speed);

            moveDir *= speed;
        }
        else
        {
            moveDir *= airSpeed;
        }

        // Vertical velocity (gravity + jump)
        moveDir.y = VerticalForceCalculator();

        controller.Move(moveDir * Time.deltaTime);

        // Consume the jump request AFTER move, so each Space press triggers exactly one jump
        jumpRequested = false;
    }

    // ─────────────────────────────────────────────
    //  VERTICAL FORCE CALCULATOR
    // ─────────────────────────────────────────────
    float VerticalForceCalculator()
    {
        // Only consider the player grounded for jump purposes if cooldown has elapsed
        bool stableGrounded = controller.isGrounded
                              && Time.time - lastJumpTime > jumpCooldown;

        if (stableGrounded)
        {
            // Stick to ground with a small downward force
            verticalVelocity = -2f;

            // Jump only fires here, only when stable-grounded, only when requested
            if (jumpRequested)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * gravity * 2f);
                lastJumpTime     = Time.time;
            }
        }
        else
        {
            // In the air — apply gravity continuously
            verticalVelocity -= gravity * Time.deltaTime;
        }

        return verticalVelocity;
    }
}