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
