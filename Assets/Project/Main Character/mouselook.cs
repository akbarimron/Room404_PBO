using UnityEngine;
using UnityEngine.InputSystem;

public class mouselook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 10f;
    
    public Transform playerBody;
    
    private float xRotation = 0f;

    void Awake()
    {
        ResolvePlayerBody();
    }

    void Start()
    {
        ResolvePlayerBody();
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
        ResolvePlayerBody();

        if (playerBody == null)
        {
            Debug.LogError("playerBody is not assigned! Drag your Player object into the playerBody field in the Inspector.");
            return;
        }

        // Mouse Look
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            
            float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            // Rotate camera up/down
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Rotate player body left/right (mouse)
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
