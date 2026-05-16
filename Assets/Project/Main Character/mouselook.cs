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
            settingsUI = FindObjectOfType<SettingsUI>();
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
        if (settingsUI != null && settingsUI.IsSettingsOpen())
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
}
