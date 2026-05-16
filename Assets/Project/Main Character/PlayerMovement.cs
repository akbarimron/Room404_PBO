using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private CharacterController controller;

    [Header("UI Reference")]
    [SerializeField] private Slider staminaBar;
    [SerializeField] private SettingsUI settingsUI;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f; // Berkurang per detik
    [SerializeField] private float staminaRegenRate = 15f; // Bertambah per detik
    [Range(0, 100)] 
    [SerializeField] private float minStaminaToSprint = 20f; // Stamina minimal untuk mulai lari lagi setelah habis
    
    private float currentStamina;
    private bool isExhausted = false; // Status jika stamina benar-benar habis

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
    private bool canMove = true;

    void Start()
    {
        currentStamina = maxStamina;

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

        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }

        if (settingsUI == null)
            settingsUI = FindObjectOfType<SettingsUI>();

        if (settingsUI == null)
            Debug.LogWarning("SettingsUI not found!");

        if (SettingsManager.Instance != null)
        {
            var settings = SettingsManager.Instance.GetSettings();
            walkSpeed = settings.walkSpeed;
            sprintSpeed = settings.sprintSpeed;
            bobSpeed = settings.bobSpeed;
            bobAmount = settings.bobAmount;
        }
    }

    void Update()
    {
        HandleGroundCheck();
        HandleInputAndMovement();
        HandleStamina();
        UpdateCameraBob();
        UpdateUI();
    }

    void UpdateUI()
    {
        if (staminaBar != null)
        {
            // Mengisi slider (0 sampai 1)
            staminaBar.value = currentStamina / maxStamina;

            // Opsional: Ubah warna bar jadi merah kalau habis (Exhausted)
            Image fillImage = staminaBar.fillRect.GetComponent<Image>();
            if (isExhausted)
                fillImage.color = Color.red;
            else
                fillImage.color = Color.green;
        }
    }

    void HandleGroundCheck()
    {
        if (groundLayer == 0)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance);
        else
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    void HandleInputAndMovement()
    {
        canMove = (settingsUI == null || !settingsUI.IsSettingsOpen());

        if (!canMove)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float moveZ = 0f;
        float moveX = 0f;

        // Input movement
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;

        isMoving = (moveZ != 0 || moveX != 0) && isGrounded;

        // Logika Sprinting dengan pengecekan stamina
        bool isSprintInput = keyboard.leftShiftKey.isPressed;
        
        // Pemain hanya bisa lari jika: mencet shift, lagi gerak, tidak lelah (exhausted), dan stamina > 0
        bool canSprint = isSprintInput && isMoving && !isExhausted && currentStamina > 0;

        currentSpeed = canSprint ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.forward * moveZ + transform.right * moveX).normalized * currentSpeed;

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        move.y = velocity.y;

        controller.Move(move * Time.deltaTime);
    }

    void HandleStamina()
    {
        bool isSprinting = currentSpeed == sprintSpeed && isMoving;

        if (isSprinting)
        {
            // Kurangi stamina saat lari
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true; // Masuk fase capek
            }
        }
        else
        {
            // Tambah stamina saat jalan/diam
            currentStamina += staminaRegenRate * Time.deltaTime;
            
            // Jika stamina sudah pulih ke batas tertentu, hilangkan status exhausted
            if (isExhausted && currentStamina >= minStaminaToSprint)
            {
                isExhausted = false;
            }
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        
        // Debug untuk melihat stamina di console (opsional)
        // Debug.Log($"Stamina: {Mathf.RoundToInt(currentStamina)} | Exhausted: {isExhausted}");
    }

    void UpdateCameraBob()
    {
        if (mainCamera == null) return;

        if (isMoving)
        {
            // Mempercepat bobbing saat lari
            float speedMultiplier = (currentSpeed == sprintSpeed) ? 1.5f : 1f;
            bobTimer += Time.deltaTime * (bobSpeed * speedMultiplier);
            
            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            mainCamera.localPosition = cameraOriginalPos + new Vector3(0, bobY, 0);
        }
        else
        {
            bobTimer = 0f;
            mainCamera.localPosition = Vector3.Lerp(mainCamera.localPosition, cameraOriginalPos, Time.deltaTime * 5f);
        }
    }

    // Getter untuk UI jika ingin membuat Slider Stamina
    public float GetStaminaNormalized()
    {
        return currentStamina / maxStamina;
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