using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  REFERENCES
    // ─────────────────────────────────────────────
    [Header("References")]
    [SerializeField] Animator animator;
    [SerializeField] Transform playerObj;
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
    [SerializeField] float gravity   = 16f;
    float verticalVelocity;

    // ─────────────────────────────────────────────
    //  JUMP SETTINGS
    // ─────────────────────────────────────────────
    [Header("Jump Settings")]
    [SerializeField] float jumpHeight = 2f;
    bool jumped;

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
        if (animator == null) animator = GetComponent<Animator>();
    }

    // ─────────────────────────────────────────────
    //  UPDATE — input reading
    // ─────────────────────────────────────────────
    void Update()
    {
        // Read WASD input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputMove = new Vector3(h, 0f, v).normalized;

        // Sprint — hold Left Shift
        sprint = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;

        // Jump — Space
        if (Input.GetButtonDown("Jump"))
            Jump();

        // Rotate player model toward movement direction
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
    //  VERTICAL FORCE CALCULATOR
    // ─────────────────────────────────────────────
    float VerticalForceCalculator()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -1f;

            if (jumped)
                verticalVelocity = Mathf.Sqrt(jumpHeight * gravity * 2f);
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        return verticalVelocity;
    }

    // ─────────────────────────────────────────────
    //  GROUND MOVEMENT
    // ─────────────────────────────────────────────
    void GroundMovement()
    {
        moveDir = inputMove;

        animator.SetBool("Grounded", true);
        animator.SetBool("FreeFall", false);
        animator.SetBool("Jumping",     false);

        if (controller.isGrounded)
        {
            jumped = false;

            if (sprint > 0)
            {
                if (moveDir != Vector3.zero)
                {
                    speed = Mathf.Lerp(speed, sprintSpeed, sprintTransitSpeed * Time.deltaTime);
                    animator.SetFloat("Speed", speed);
                }
                else
                {
                    speed = Mathf.Lerp(speed, idle, sprintTransitSpeed * Time.deltaTime);
                    animator.SetFloat("Speed", speed);
                }
            }
            else
            {
                if (moveDir != Vector3.zero)
                {
                    speed = Mathf.Lerp(speed, walkSpeed, sprintTransitSpeed * Time.deltaTime);
                    animator.SetFloat("Speed", speed);
                }
                else
                {
                    speed = Mathf.Lerp(speed, idle, sprintTransitSpeed * Time.deltaTime);
                    animator.SetFloat("Speed", speed);
                }
            }

            moveDir *= speed;
        }
        else if (!jumped && !controller.isGrounded)
        {
            animator.SetBool("FreeFall", true);
            moveDir *= airSpeed;
        }

        moveDir.y = VerticalForceCalculator();
        controller.Move(moveDir * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  JUMP
    // ─────────────────────────────────────────────
    void Jump()
    {
        if (!controller.isGrounded) return;

        jumped           = true;
        verticalVelocity = Mathf.Sqrt(jumpHeight * gravity * 2f);

        animator.SetBool("Grounded", false);
        animator.SetBool("Jump",     true);
    }
}