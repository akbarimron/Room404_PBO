using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private CharacterController controller;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Camera Bobbing")]
    [SerializeField] private Transform mainCamera;
    [SerializeField] private float bobSpeed = 8f;
    [SerializeField] private float bobAmount = 0.3f;

    private Vector3 velocity = Vector3.zero;
    private bool isGrounded;
    private float currentSpeed;
    private Vector3 cameraOriginalPos;
    private float bobTimer = 0f;
    private bool isMoving = false;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (mainCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                mainCamera = mainCam.transform;
        }

        if (mainCamera != null)
            cameraOriginalPos = mainCamera.localPosition;

        // Auto-create ground check jika belum ada
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }

        // Debugging
        if (groundCheck == null)
            Debug.LogError("Ground check tidak ada!");
    }

    void Update()
    {
        // Ground check - gunakan default layer jika tidak di-assign
        if (groundLayer == 0)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance);
        else
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        Debug.Log($"Is Grounded: {isGrounded}");

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Get input
        Keyboard keyboard = Keyboard.current;
        float moveZ = 0f;
        float moveX = 0f;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                moveZ += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                moveZ -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                moveX += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                moveX -= 1f;
        }

        isMoving = (moveZ != 0 || moveX != 0) && isGrounded;

        // Check sprint
        bool isSprinting = keyboard != null && keyboard.leftShiftKey.isPressed;
        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Move forward/backward dan left/right (relative to player's facing direction)
        Vector3 moveForward = transform.forward * moveZ * currentSpeed;
        Vector3 moveRight = transform.right * moveX * currentSpeed;
        Vector3 move = moveForward + moveRight;

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Jump - DISABLED
        /*
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            Debug.Log("JUMPING!");
        }
        */

        // Apply movement
        move.y = velocity.y;
        controller.Move(move * Time.deltaTime);

        // Camera bobbing
        UpdateCameraBob();
    }

    void UpdateCameraBob()
    {
        if (mainCamera == null) return;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            mainCamera.localPosition = cameraOriginalPos + new Vector3(0, bobY, 0);
            Debug.Log($"Bobbing: {bobY}, IsMoving: {isMoving}");
        }
        else
        {
            bobTimer = 0f;
            mainCamera.localPosition = cameraOriginalPos;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
