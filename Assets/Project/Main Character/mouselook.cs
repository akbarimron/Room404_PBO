using UnityEngine;
using UnityEngine.InputSystem;

public class mouselook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 5f;

    public Transform playerBody;
    private SettingsUI settingsUI;

    private float xRotation = 0f;

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
            playerBody.Rotate(Vector3.up * mouseX);
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
}
