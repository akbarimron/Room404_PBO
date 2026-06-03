using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private CharacterController controller;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchSpeed = 2.2f;
    [SerializeField] private float crouchHeight = 1.0f;
    private float standingHeight = 2.0f;
    private bool isCrouching = false;

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

    public bool IsSprinting => currentSpeed == sprintSpeed && isMoving;
    public bool IsWalking => currentSpeed == walkSpeed && isMoving;
    public bool IsCrouching => isCrouching;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Stairs")]
    [SerializeField] private float stairStepOffset = 0.5f;
    [SerializeField] private float stairSlopeLimit = 50f;
    [SerializeField] private float stairAssistSpeed = 0f;
    [SerializeField] private float stairStickToGroundForce = 8f;
    [SerializeField] private float stairContactGraceTime = 0.2f;
    [SerializeField] private LayerMask stairLayer;
    [SerializeField] private string[] stairNameKeywords = { "tangga", "stairs", "stair" };

    [Header("Camera Bobbing")]
    [SerializeField] private Transform mainCamera;
    [SerializeField] private float bobSpeed = 8f;
    [SerializeField] private float bobAmount = 0.3f;

    private Vector3 velocity = Vector3.zero;
    private bool isGrounded;
    private bool wasGrounded;
    private float currentSpeed;
    private Vector3 cameraOriginalPos;
    private float bobTimer = 0f;
    private bool isMoving = false;
    private bool canMove = true;
    private Collider activeStair;
    private float lastStairContactTime = -1f;

    void Start()
    {
        currentStamina = maxStamina;
        gravity = -25f;
        stairStickToGroundForce = 20f;

        if (controller == null)
            controller = GetComponent<CharacterController>();

        standingHeight = controller != null ? controller.height : 2.0f;

        if (controller != null)
        {
            controller.stepOffset = Mathf.Max(controller.stepOffset, stairStepOffset);
            controller.slopeLimit = Mathf.Max(controller.slopeLimit, stairSlopeLimit);
        }

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
            settingsUI = FindSettingsUI();

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
        ApplyRuntimeSettings();
        HandleGroundCheck();
        HandleInputAndMovement();
        HandleStamina();
        UpdateCameraBob();
        UpdateUI();
    }

    void ApplyRuntimeSettings()
    {
        if (SettingsManager.Instance == null)
            return;

        var settings = SettingsManager.Instance.GetSettings();
        walkSpeed = settings.walkSpeed;
        sprintSpeed = settings.sprintSpeed;
        bobSpeed = settings.bobSpeed;
        bobAmount = settings.bobAmount;
    }

    void UpdateUI()
    {
        if (staminaBar == null)
            return;

        // Mengisi slider (0 sampai 1)
        staminaBar.value = currentStamina / maxStamina;

        if (staminaBar.fillRect != null)
        {
            // Opsional: Ubah warna bar jadi merah kalau habis (Exhausted)
            Image fillImage = staminaBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = isExhausted ? Color.red : Color.green;
        }
    }

    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;

        bool sphereGrounded;
        if (groundLayer == 0)
            sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance);
        else
            sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        // Combine sphere check with controller's physical grounding for robustness
        isGrounded = sphereGrounded || (controller != null && controller.isGrounded);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    void HandleInputAndMovement()
    {
        canMove = !IsSettingsOpen();

        if (!canMove)
        {
            return;
        }

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

        // Tombol jongkok (Ctrl kiri atau C)
        bool crouchInput = keyboard.ctrlKey.isPressed || keyboard.cKey.isPressed;
        isCrouching = crouchInput && isGrounded;

        if (isCrouching)
        {
            controller.height = crouchHeight;
            controller.center = new Vector3(0f, crouchHeight / 2f, 0f);
        }
        else
        {
            controller.height = standingHeight;
            controller.center = new Vector3(0f, standingHeight / 2f, 0f);
        }

        // Sesuaikan tinggi kamera saat jongkok secara halus
        if (mainCamera != null)
        {
            float targetCamY = isCrouching ? cameraOriginalPos.y - 0.7f : cameraOriginalPos.y;
            Vector3 targetCamPos = new Vector3(cameraOriginalPos.x, targetCamY, cameraOriginalPos.z);
            mainCamera.localPosition = Vector3.Lerp(mainCamera.localPosition, targetCamPos, Time.deltaTime * 8f);
        }

        // Logika Sprinting dengan pengecekan stamina
        bool isSprintInput = keyboard.leftShiftKey.isPressed;
        
        // Pemain hanya bisa lari jika: mencet shift, lagi gerak, tidak lelah (exhausted), dan stamina > 0, serta tidak sedang jongkok
        bool canSprint = isSprintInput && isMoving && !isExhausted && currentStamina > 0 && !isCrouching;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = canSprint ? sprintSpeed : walkSpeed;
        }

        Vector3 horizontalMove = (transform.forward * moveZ + transform.right * moveX).normalized * currentSpeed;
        Vector3 move = horizontalMove;

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        if (ShouldStickToStairs(horizontalMove))
            velocity.y = Mathf.Min(velocity.y, -stairStickToGroundForce);

        if (ShouldAssistStairs(horizontalMove))
            velocity.y = Mathf.Max(velocity.y, stairAssistSpeed);

        move.y = velocity.y;

        controller.Move(move * Time.deltaTime);

        // Ground Snapping (to prevent floating when walking down stairs/slopes)
        if ((wasGrounded || HasRecentStairContact()) && velocity.y <= 0 && groundCheck != null)
        {
            // Increase the check range to support steeper or faster steps (minimum 1.2m)
            float checkDist = Mathf.Max(stairStepOffset, 1.2f);
            Vector3 rayStart = groundCheck.position + Vector3.up * 0.5f; // Start 0.5m above feet to avoid starting below the floor
            LayerMask mask = ~0; // Check all layers to ensure we snap to stairs regardless of layer mismatches
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, checkDist + 0.5f, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root != transform.root)
                {
                    // Calculate base snap distance, then add 0.05m extra push to force solid collision contact
                    float snapDistance = (hit.distance - 0.5f) + 0.05f;
                    if (snapDistance > 0.01f)
                    {
                        controller.Move(Vector3.down * snapDistance);
                        isGrounded = true;
                        velocity.y = -2f;
                    }
                }
            }
        }
    }

    private bool ShouldAssistStairs(Vector3 horizontalMove)
    {
        return stairAssistSpeed > 0f && HasRecentStairContact() && IsMovingUpStairs(horizontalMove);
    }

    private bool ShouldStickToStairs(Vector3 horizontalMove)
    {
        return HasRecentStairContact() && horizontalMove.sqrMagnitude > 0.01f && !IsMovingUpStairs(horizontalMove);
    }

    private bool IsMovingUpStairs(Vector3 horizontalMove)
    {
        if (horizontalMove.sqrMagnitude < 0.01f || groundCheck == null)
            return false;

        // Cast a ray slightly forward in the movement direction (e.g. 0.3m) and look at the ground height
        Vector3 forwardDir = horizontalMove.normalized;
        Vector3 checkOrigin = groundCheck.position + forwardDir * 0.3f + Vector3.up * 0.5f;
        
        if (Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit hit, 1.0f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root != transform.root)
            {
                // If the hit point is higher than groundCheck.position, the ground in front is higher (going UP)
                float heightDiff = hit.point.y - groundCheck.position.y;
                return heightDiff > 0.05f; // Threshold of 5cm
            }
        }
        return false;
    }

    private bool HasRecentStairContact()
    {
        return activeStair != null && Time.time - lastStairContactTime <= stairContactGraceTime;
    }

    private bool IsSettingsOpen()
    {
        if (settingsUI == null)
            settingsUI = FindSettingsUI();

        if (settingsUI != null)
            return settingsUI.IsSettingsOpen();

        return SettingsManager.Instance != null && SettingsManager.Instance.IsSettingsOpen();
    }

    private SettingsUI FindSettingsUI()
    {
        SettingsUI[] foundSettingsUis = FindObjectsByType<SettingsUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return foundSettingsUis.Length > 0 ? foundSettingsUis[0] : null;
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
            // Mempercepat bobbing saat lari, melambat saat jongkok
            float speedMultiplier = (currentSpeed == sprintSpeed) ? 1.5f : (isCrouching ? 0.5f : 1f);
            bobTimer += Time.deltaTime * (bobSpeed * speedMultiplier);
            
            float bobY = Mathf.Sin(bobTimer) * (isCrouching ? bobAmount * 0.4f : bobAmount);
            float currentCamY = isCrouching ? cameraOriginalPos.y - 0.7f : cameraOriginalPos.y;
            mainCamera.localPosition = new Vector3(cameraOriginalPos.x, currentCamY + bobY, cameraOriginalPos.z);
        }
        else
        {
            bobTimer = 0f;
            float currentCamY = isCrouching ? cameraOriginalPos.y - 0.7f : cameraOriginalPos.y;
            mainCamera.localPosition = Vector3.Lerp(mainCamera.localPosition, new Vector3(cameraOriginalPos.x, currentCamY, cameraOriginalPos.z), Time.deltaTime * 5f);
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

    private void OnTriggerEnter(Collider other)
    {
        TrySetActiveStair(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySetActiveStair(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == activeStair)
            activeStair = null;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TrySetActiveStair(hit.collider);
    }

    private void TrySetActiveStair(Collider other)
    {
        if (!IsStair(other))
            return;

        activeStair = other;
        lastStairContactTime = Time.time;
    }

    private bool IsStair(Collider other)
    {
        if (other == null)
            return false;

        if (stairLayer.value != 0 && (stairLayer.value & (1 << other.gameObject.layer)) != 0)
            return true;

        Transform current = other.transform;
        while (current != null)
        {
            string objectName = current.name.ToLowerInvariant();
            string objectTag = current.gameObject.tag.ToLowerInvariant();

            for (int i = 0; i < stairNameKeywords.Length; i++)
            {
                string keyword = stairNameKeywords[i];
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;

                keyword = keyword.ToLowerInvariant();
                if (objectName.Contains(keyword) || objectTag.Contains(keyword))
                    return true;
            }

            current = current.parent;
        }

        return false;
    }

    // Tambahkan fungsi publik ini di bagian paling bawah script PlayerMovement.cs
    public void IncreaseStamina(float amount)
    {
        currentStamina += amount;
        
        // Batasi agar tidak melebihi kapasitas maksimal stamina
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        
        // Jika stamina bertambah melampaui batas minimal lari, matikan status exhausted
        if (isExhausted && currentStamina >= minStaminaToSprint)
        {
            isExhausted = false;
        }

        Debug.Log("Minuman dikonsumsi! Stamina saat ini: " + Mathf.RoundToInt(currentStamina));
    }
}
