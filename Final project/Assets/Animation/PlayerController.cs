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
    [SerializeField] float jumpHeight        = 4f;
    [SerializeField] float jumpCooldown      = 0.4f;
    // Coyote time: how long after losing ground contact we still treat her as "grounded".
    // This is what kills the bump-induced fake-jumps. Increase if she still flickers.
    [SerializeField] float coyoteTime        = 0.15f;
    bool  jumpRequested;
    float lastJumpTime    = -999f;
    float lastGroundedTime = -999f;

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

        // Jump input — queue it; actual jump runs in FixedUpdate
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
        // Update grounded-time tracking
        if (controller.isGrounded)
            lastGroundedTime = Time.time;

        GroundMovement();
    }

    // ─────────────────────────────────────────────
    //  Helper: is she "effectively grounded" right now?
    //  This is the KEY fix: treats her as grounded if she was on the ground
    //  any time in the last `coyoteTime` seconds. Brief bump-induced gaps
    //  in controller.isGrounded no longer cause the animator to flicker.
    // ─────────────────────────────────────────────
    bool IsEffectivelyGrounded()
    {
        return Time.time - lastGroundedTime <= coyoteTime
               && Time.time - lastJumpTime > jumpCooldown;
    }

    // ─────────────────────────────────────────────
    //  GROUND MOVEMENT
    // ─────────────────────────────────────────────
    void GroundMovement()
    {
        moveDir = inputMove;

        bool effectivelyGrounded = IsEffectivelyGrounded();

        // Animator states — these now use coyote-time grounded, so tiny bumps
        // don't flip her into "FreeFall" for a frame.
        animator.SetBool("Grounded", effectivelyGrounded);
        animator.SetBool("FreeFall", !effectivelyGrounded && verticalVelocity < 0f);
        animator.SetBool("Jumping",  !effectivelyGrounded && verticalVelocity > 0f);

        if (effectivelyGrounded)
        {
            // Pick target speed
            float targetSpeed = (sprint > 0) ? sprintSpeed : walkSpeed;
            if (moveDir == Vector3.zero) targetSpeed = idle;

            speed = Mathf.Lerp(speed, targetSpeed, sprintTransitSpeed * Time.deltaTime);
            animator.SetFloat("Speed", speed);

            moveDir *= speed;
        }
        else
        {
            // Truly in the air — preserve horizontal momentum at air speed
            moveDir *= airSpeed;
        }

        // Vertical velocity (gravity + jump)
        moveDir.y = VerticalForceCalculator();

        controller.Move(moveDir * Time.deltaTime);

        // Consume the jump request after move
        jumpRequested = false;
    }

    // ─────────────────────────────────────────────
    //  VERTICAL FORCE CALCULATOR
    // ─────────────────────────────────────────────
    float VerticalForceCalculator()
    {
        bool effectivelyGrounded = IsEffectivelyGrounded();

        if (effectivelyGrounded)
        {
            // Glue her to the ground. Stronger downward push helps her stay
            // stuck on slopes and tiny bumps without bouncing.
            verticalVelocity = -5f;

            // Process jump
            if (jumpRequested)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * gravity * 2f);
                lastJumpTime     = Time.time;
                // Force her into the air immediately so coyote time doesn't
                // re-ground her on the next frame.
                lastGroundedTime = -999f;
            }
        }
        else
        {
            // Apply gravity continuously
            verticalVelocity -= gravity * Time.deltaTime;
        }

        return verticalVelocity;
    }
}