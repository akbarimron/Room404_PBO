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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen;

    public bool IsOpen => isOpen;
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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (audioSource != null)
        {
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.playOnAwake = false;
            audioSource.minDistance = 3.0f;  // Audibility setup
            audioSource.maxDistance = 20.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = 1.0f;
        }

#if UNITY_EDITOR
        if (openSound == null)
        {
            openSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/openDoor.mp3");
            if (openSound != null)
            {
                Debug.Log($"<color=green>[InteractiveDoor]</color> Loaded fallback openSound dynamically: {openSound.name}");
            }
        }
        if (closeSound == null)
        {
            closeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/openDoor.mp3");
            if (closeSound != null)
            {
                Debug.Log($"<color=green>[InteractiveDoor]</color> Loaded fallback closeSound dynamically: {closeSound.name}");
            }
        }
#endif
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

        bool shouldPlaySound = true;

        // Check if opened/closed by ghost (not tagged Player)
        if (interactor != null && !interactor.CompareTag("Player"))
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerObj.transform.position);
                if (distToPlayer > 12.0f)
                {
                    shouldPlaySound = false;
                    Debug.Log($"<color=cyan>[InteractiveDoor-Audio]</color> Ghost interacted with door {gameObject.name} far from player ({distToPlayer:F1}m). Silent.");
                }
            }
        }

        if (shouldPlaySound && audioSource != null)
        {
            AudioClip clipToPlay = isOpen ? closeSound : openSound;
            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                if (clipToPlay.name == "openDoor" || clipToPlay.name == "openDoor_trimmed")
                {
                    audioSource.time = 3.138f;
                }
                else
                {
                    audioSource.time = 0f;
                }
                audioSource.Play();
                Debug.Log($"<color=green>[InteractiveDoor-Audio]</color> Played sound: {clipToPlay.name} (isOpen: {isOpen}) on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[InteractiveDoor-Audio]</color> Sound clip is null on {gameObject.name}");
            }
        }

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
