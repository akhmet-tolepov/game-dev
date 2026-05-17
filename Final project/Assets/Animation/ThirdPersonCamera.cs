using UnityEngine;

/// <summary>
/// Third-person follow camera. Orbits around the target with mouse input,
/// smoothly follows the target's position, and zooms with the scroll wheel.
///
/// SETUP:
/// 1. Attach this script to your Main Camera.
/// 2. Drag the player (Ainura) into the "Target" slot in the Inspector.
/// 3. Press Play. Move mouse to orbit, scroll to zoom.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;          // The player to follow (drag Ainura here)
    [SerializeField] Vector3 targetOffset = new Vector3(0f, 1.5f, 0f); // Aim at chest, not feet

    [Header("Distance")]
    [SerializeField] float distance    = 5f;     // Current distance
    [SerializeField] float minDistance = 2f;
    [SerializeField] float maxDistance = 10f;
    [SerializeField] float zoomSpeed   = 4f;

    [Header("Rotation")]
    [SerializeField] float mouseSensitivityX = 200f;
    [SerializeField] float mouseSensitivityY = 150f;
    [SerializeField] float minPitch = -30f;      // How far down you can look
    [SerializeField] float maxPitch = 70f;       // How far up you can look

    [Header("Smoothing")]
    [SerializeField] float positionSmoothing = 10f;
    [SerializeField] float rotationSmoothing = 15f;

    [Header("Collision")]
    [SerializeField] bool  avoidWallClipping = true;
    [SerializeField] float collisionBuffer   = 0.3f;
    [SerializeField] LayerMask collisionMask = ~0; // Everything by default

    // Internal state
    float yaw;
    float pitch = 15f; // Start with a slight downward angle

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: No target assigned! Drag Ainura into the Target slot.");
            enabled = false;
            return;
        }

        // Lock cursor to center of screen and hide it (typical third-person feel)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Initialize yaw from target's current facing
        yaw = target.eulerAngles.y;
    }

    void Update()
    {
        // Allow user to unlock cursor with Escape, re-lock with mouse click
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        // Mouse input — only orbit when cursor is locked
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw   += Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Scroll to zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            distance -= scroll * zoomSpeed;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Compute the desired rotation and the position behind the target.
        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    pivot           = target.position + targetOffset;
        Vector3    desiredPosition = pivot - desiredRotation * Vector3.forward * distance;

        // Wall avoidance: raycast from the player toward the desired camera position.
        // If something blocks the view, pull the camera in to that point.
        if (avoidWallClipping)
        {
            Vector3 direction = desiredPosition - pivot;
            float   rayLength = direction.magnitude;
            if (Physics.Raycast(pivot, direction.normalized, out RaycastHit hit, rayLength, collisionMask))
            {
                desiredPosition = hit.point - direction.normalized * collisionBuffer;
            }
        }

        // Smoothly interpolate to the desired position and rotation.
        transform.position = Vector3.Lerp(transform.position, desiredPosition,
                                          positionSmoothing * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                                              rotationSmoothing * Time.deltaTime);
    }
}