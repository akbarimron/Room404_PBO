using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractionBootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsurePlayerComponents();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePlayerComponents();
    }

    private static void EnsurePlayerComponents()
    {
        Camera mainCamera = Camera.main;
        GameObject targetObj = null;

        if (mainCamera != null)
        {
            targetObj = mainCamera.gameObject;
        }
        else
        {
            PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                targetObj = playerMovement.gameObject;
            }
        }

        if (targetObj != null)
        {
            if (targetObj.GetComponent<PlayerInteraction>() == null && Object.FindFirstObjectByType<PlayerInteraction>() == null)
            {
                targetObj.AddComponent<PlayerInteraction>();
            }

            if (targetObj.GetComponent<FlashlightController>() == null && Object.FindFirstObjectByType<FlashlightController>() == null)
            {
                targetObj.AddComponent<FlashlightController>();
            }

            if (targetObj.GetComponent<PlayerGhostEffect>() == null && Object.FindFirstObjectByType<PlayerGhostEffect>() == null)
            {
                targetObj.AddComponent<PlayerGhostEffect>();
            }
        }
    }
}
