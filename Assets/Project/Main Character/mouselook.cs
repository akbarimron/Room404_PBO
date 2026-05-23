using UnityEngine;
using UnityEngine.InputSystem;

public class mouselook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 5f;

    public Transform playerBody;
    private SettingsUI settingsUI;

    private float xRotation = 0f;

    private bool clampHorizontal = false;
    private float centerYaw = 0f;
    private float yawRange = 70f;

    void Awake()
    {
        ResolvePlayerBody();
    }

    void Start()
    {
        ResolvePlayerBody();
        if (settingsUI == null)
            settingsUI = FindSettingsUI();
    }

    private void ResolvePlayerBody()
    {
        if (playerBody != null)
        {
            return;
        }

        if (transform.parent != null)
        {
            playerBody = transform.parent;
            return;
        }

        GameObject playerObject = GameObject.Find("First Person Player");
        if (playerObject != null)
        {
            playerBody = playerObject.transform;
        }
    }

    void Update()
    {
        if (IsSettingsOpen())
            return;

        ResolvePlayerBody();

        if (playerBody == null)
        {
            Debug.LogError("playerBody is not assigned! Drag your Player object into the playerBody field in the Inspector.");
            return;
        }

        // Get sensitivity from SettingsManager
        float currentSensitivity = SettingsManager.Instance != null
            ? SettingsManager.Instance.GetMouseSensitivity()
            : mouseSensitivity;

        // Mouse Look
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();

            float mouseX = mouseDelta.x * currentSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * currentSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            // Rotate camera up/down
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Rotate player body left/right (mouse)
            if (clampHorizontal)
            {
                float currentYaw = playerBody.eulerAngles.y;
                float newYaw = currentYaw + mouseX;
                float deltaAngle = Mathf.DeltaAngle(centerYaw, newYaw);
                deltaAngle = Mathf.Clamp(deltaAngle, -yawRange, yawRange);
                playerBody.rotation = Quaternion.Euler(0f, centerYaw + deltaAngle, 0f);
            }
            else
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }
        }
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

    public void SetRotation(float pitch, float yaw)
    {
        xRotation = pitch;
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    public void EnableHidingClamp(float targetCenterYaw, float range = 70f)
    {
        clampHorizontal = true;
        centerYaw = targetCenterYaw;
        yawRange = range;
    }

    public void DisableHidingClamp()
    {
        clampHorizontal = false;
    }
}
