using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class LockerController : MonoBehaviour
{
    [Header("Locker Spots")]
    [SerializeField] private Transform hidingSpot;
    [SerializeField] private Transform exitSpot;

    [Header("Prompts")]
    [SerializeField] private string hidePrompt = "Press [E] to hide in locker";
    [SerializeField] private string exitPrompt = "Press [E] to exit locker";

    [Header("Hiding Settings")]
    [SerializeField] private float transitionSpeed = 5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;

    private bool isOccupied = false;
    private GameObject hidingPlayer;
    private bool isTransitioning = false;

    public bool IsOccupied => isOccupied;
    public string Prompt => isOccupied ? exitPrompt : hidePrompt;

    public void ForceEject()
    {
        if (isOccupied && !isTransitioning)
        {
            StartCoroutine(ExitLocker());
        }
    }

    void Awake()
    {
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
        if (enterSound == null)
        {
            enterSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/openDoor.mp3");
            if (enterSound != null)
            {
                Debug.Log($"<color=green>[LockerController]</color> Loaded fallback enterSound dynamically: {enterSound.name}");
            }
        }
        if (exitSound == null)
        {
            exitSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/openDoor.mp3");
            if (exitSound != null)
            {
                Debug.Log($"<color=green>[LockerController]</color> Loaded fallback exitSound dynamically: {exitSound.name}");
            }
        }
#endif
    }

    void Update()
    {
        if (isOccupied && !isTransitioning)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                StartCoroutine(ExitLocker());
            }
        }
    }

    public void ToggleHiding(GameObject player)
    {
        if (isTransitioning) return;

        if (!isOccupied)
        {
            StartCoroutine(EnterLocker(player));
        }
        else
        {
            StartCoroutine(ExitLocker());
        }
    }

    private IEnumerator EnterLocker(GameObject player)
    {
        isTransitioning = true;
        hidingPlayer = player;
        isOccupied = true;

        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.HideText();
        }

        if (audioSource != null && enterSound != null)
        {
            audioSource.clip = enterSound;
            if (enterSound.name == "openDoor" || enterSound.name == "openDoor_trimmed")
            {
                audioSource.time = 3.138f;
            }
            else
            {
                audioSource.time = 0f;
            }
            audioSource.Play();
            Debug.Log($"<color=green>[LockerController-Audio]</color> Played enterSound: {enterSound.name} on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[LockerController-Audio]</color> enterSound is null on {gameObject.name}");
        }

        // Set hiding state on PlayerHealth
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.isHiding = true;
        }

        // Disable player controls
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        CharacterController controller = player.GetComponent<CharacterController>();

        if (movement != null) movement.enabled = false;
        if (controller != null) controller.enabled = false;

        // Disable mouselook during transition
        mouselook look = player.GetComponentInChildren<mouselook>();
        if (look != null) look.enabled = false;

        // Change tag to avoid AI detection
        player.tag = "Untagged";

        // Determine targets
        Vector3 targetPos = hidingSpot != null ? hidingSpot.position : transform.position;
        
        // Create a clean level rotation (0 pitch/roll) facing the locker door (270 degrees from locker rotation to face the exit door)
        float lockerYaw = transform.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, lockerYaw + 270f, 0f);

        float elapsed = 0f;
        Vector3 startPos = player.transform.position;
        Quaternion startRot = player.transform.rotation;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            player.transform.position = Vector3.Lerp(startPos, targetPos, elapsed);
            player.transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed);
            yield return null;
        }

        player.transform.position = targetPos;
        player.transform.rotation = targetRot;

        // Reset and re-enable mouselook facing the locker door, clamping rotation to ±70 degrees
        if (look != null)
        {
            look.SetRotation(0f, targetRot.eulerAngles.y);
            look.EnableHidingClamp(targetRot.eulerAngles.y, 70f);
            look.enabled = true;
        }

        // Show exit prompt persistently
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.ShowText(exitPrompt);
        }

        isTransitioning = false;
    }

    private IEnumerator ExitLocker()
    {
        if (hidingPlayer == null) yield break;

        isTransitioning = true;

        if (audioSource != null && exitSound != null)
        {
            audioSource.clip = exitSound;
            if (exitSound.name == "openDoor" || exitSound.name == "openDoor_trimmed")
            {
                audioSource.time = 3.138f;
            }
            else
            {
                audioSource.time = 0f;
            }
            audioSource.Play();
            Debug.Log($"<color=green>[LockerController-Audio]</color> Played exitSound: {exitSound.name} on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[LockerController-Audio]</color> exitSound is null on {gameObject.name}");
        }

        // Restore tag and hiding state immediately at the start of exit
        hidingPlayer.tag = "Player";
        PlayerHealth health = hidingPlayer.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.isHiding = false;
        }

        // Hide interaction text prompt
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.HideText();
        }

        // Disable mouselook during transition
        mouselook look = hidingPlayer.GetComponentInChildren<mouselook>();
        if (look != null) look.enabled = false;

        // Determine target position and make sure it faces away from the locker
        Vector3 targetPos = exitSpot != null ? exitSpot.position : (transform.position + transform.forward * 1.2f);
        
        // Create a clean level rotation (0 pitch/roll) facing the room (270 degrees from locker rotation)
        float lockerYaw = transform.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, lockerYaw + 270f, 0f);

        float elapsed = 0f;
        Vector3 startPos = hidingPlayer.transform.position;
        Quaternion startRot = hidingPlayer.transform.rotation;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            hidingPlayer.transform.position = Vector3.Lerp(startPos, targetPos, elapsed);
            hidingPlayer.transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed);
            yield return null;
        }

        hidingPlayer.transform.position = targetPos;
        hidingPlayer.transform.rotation = targetRot;

        // Re-enable components
        PlayerMovement movement = hidingPlayer.GetComponent<PlayerMovement>();
        CharacterController controller = hidingPlayer.GetComponent<CharacterController>();

        if (movement != null) movement.enabled = true;
        if (controller != null) controller.enabled = true;

        // Reset and re-enable mouselook facing away from the locker
        if (look != null)
        {
            look.DisableHidingClamp();
            look.SetRotation(0f, targetRot.eulerAngles.y);
            look.enabled = true;
        }

        hidingPlayer = null;
        isOccupied = false;
        isTransitioning = false;
    }
}
