using UnityEngine;

public class InteractiveDoor : MonoBehaviour
{
    [Header("Door Movement")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 5f;
    [SerializeField] private bool openAwayFromPlayer = true;

    [Header("Prompt")]
    [SerializeField] private string openPrompt = "Tekan [E] - Open Door";
    [SerializeField] private string closePrompt = "Tekan [E] - Close Door";

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen;

    public string Prompt => isOpen ? closePrompt : openPrompt;

    void Awake()
    {
        if (doorTransform == null)
            doorTransform = transform;

        closedRotation = doorTransform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, 0f, openAngle);
        EnsureCollider();

        if (string.IsNullOrEmpty(openPrompt) || openPrompt.Contains("Tekan") || openPrompt.Contains("-"))
            openPrompt = "Press [E] to open the door";
        if (string.IsNullOrEmpty(closePrompt) || closePrompt.Contains("Tekan") || closePrompt.Contains("-"))
            closePrompt = "Press [E] to close the door";
    }

    void Reset()
    {
        doorTransform = transform;
        EnsureCollider();
    }

    void Update()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        doorTransform.localRotation = Quaternion.Slerp(doorTransform.localRotation, targetRotation, openSpeed * Time.deltaTime);
    }

    public void Toggle(Transform interactor)
    {
        if (!isOpen && openAwayFromPlayer && interactor != null)
            SetOpenDirection(interactor);

        isOpen = !isOpen;
    }

    private void SetOpenDirection(Transform interactor)
    {
        Vector3 toPlayer = interactor.position - doorTransform.position;
        float side = Vector3.Dot(doorTransform.right, toPlayer) >= 0f ? -1f : 1f;
        openRotation = closedRotation * Quaternion.Euler(0f, 0f, openAngle * side);
    }

    private void EnsureCollider()
    {
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider != null)
            return;

        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        boxCollider.center = transform.InverseTransformPoint(bounds.center);
        boxCollider.size = new Vector3(
            bounds.size.x / Mathf.Max(0.001f, transform.lossyScale.x),
            bounds.size.y / Mathf.Max(0.001f, transform.lossyScale.y),
            bounds.size.z / Mathf.Max(0.001f, transform.lossyScale.z)
        );
    }
}
