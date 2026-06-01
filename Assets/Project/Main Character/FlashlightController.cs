using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private Light flashlight;
    [SerializeField] private bool isOnAtStart = false;

    [Header("Input Settings")]
    [SerializeField] private Key toggleKey = Key.F;

    [Header("Close Distance Fade & Spread Settings")]
    [SerializeField] private bool enableDistanceFade = true;
    [SerializeField] private float minIntensity = 1.2f; // Kekuatan cahaya minimum agar tidak memudar gelap gulita
    [SerializeField] private float maxSpotAngle = 90f; // Senter melebar saat dekat
    [SerializeField] private float minLocalZ = -0.6f; // Ditarik ke belakang kamera untuk mencegah hotspot silau
    [SerializeField] private float fadeStartDistance = 3.0f;
    [SerializeField] private float minFadeDistance = 0.5f;
    [SerializeField] private float lerpSpeed = 10f;

    private Transform cameraTransform;
    private float baseIntensity = 3.5f;
    private float baseSpotAngle = 55f;
    private Vector3 baseLocalPosition = new Vector3(0f, 0f, 0.2f);

    void Start()
    {
        // Find main camera or use local transform
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
        }
        else
        {
            cameraTransform = transform;
        }

        // Try to find existing Light component in children
        if (flashlight == null)
        {
            flashlight = GetComponentInChildren<Light>(true);
        }

        // If still not found, check under the camera's children
        if (flashlight == null && cameraTransform != null)
        {
            flashlight = cameraTransform.GetComponentInChildren<Light>(true);
        }

        // If still not found, create a default flashlight
        if (flashlight == null)
        {
            CreateDefaultFlashlight();
        }

        // Set initial state and capture base settings
        if (flashlight != null)
        {
            baseIntensity = flashlight.intensity;
            baseSpotAngle = flashlight.spotAngle;
            baseLocalPosition = flashlight.transform.localPosition;
            flashlight.enabled = isOnAtStart;
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
        {
            ToggleFlashlight();
        }

        if (flashlight != null && flashlight.enabled && enableDistanceFade)
        {
            AdjustFlashlightProperties();
        }
    }

    public void ToggleFlashlight()
    {
        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
            Debug.Log($"<color=yellow>[FlashlightController]</color> Flashlight toggled. Active: {flashlight.enabled}");
            
            // Reset to base settings when turned off
            if (!flashlight.enabled)
            {
                flashlight.intensity = baseIntensity;
                flashlight.spotAngle = baseSpotAngle;
                flashlight.transform.localPosition = baseLocalPosition;
            }
        }
    }

    private void AdjustFlashlightProperties()
    {
        float distance = GetDistanceToWall();
        float targetIntensity = baseIntensity;
        float targetSpotAngle = baseSpotAngle;
        float targetLocalZ = baseLocalPosition.z;

        if (distance < fadeStartDistance)
        {
            // Normalize t between minFadeDistance and fadeStartDistance
            float t = Mathf.Clamp01((distance - minFadeDistance) / (fadeStartDistance - minFadeDistance));
            
            // Maintain a soft but clear intensity (at least 1.2f) to avoid fading to black
            float safeMinIntensity = Mathf.Max(1.2f, minIntensity);
            targetIntensity = Mathf.Lerp(safeMinIntensity, baseIntensity, t);
            
            // Widen the spot angle when close to diffuse the light
            targetSpotAngle = Mathf.Lerp(maxSpotAngle, baseSpotAngle, t);
            
            // Pull the light source position backwards behind the camera (minLocalZ) when close
            // This increases the physical distance between the light source and the wall,
            // which prevents hot-spot glare while maintaining excellent overall illumination.
            targetLocalZ = Mathf.Lerp(minLocalZ, baseLocalPosition.z, t);
        }

        // Smoothly adjust the flashlight properties to prevent popping
        flashlight.intensity = Mathf.Lerp(flashlight.intensity, targetIntensity, Time.deltaTime * lerpSpeed);
        flashlight.spotAngle = Mathf.Lerp(flashlight.spotAngle, targetSpotAngle, Time.deltaTime * lerpSpeed);
        
        Vector3 localPos = flashlight.transform.localPosition;
        localPos.z = Mathf.Lerp(localPos.z, targetLocalZ, Time.deltaTime * lerpSpeed);
        flashlight.transform.localPosition = localPos;
    }

    private float GetDistanceToWall()
    {
        Ray ray = new Ray(flashlight.transform.position, flashlight.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, fadeStartDistance, ~0, QueryTriggerInteraction.Ignore);
        float closestDist = fadeStartDistance;
        bool hitAnything = false;

        foreach (var hit in hits)
        {
            // Ignore player triggers and the player's own capsule/colliders
            if (hit.collider.isTrigger || hit.transform.root == transform.root)
                continue;

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                hitAnything = true;
            }
        }

        return hitAnything ? closestDist : fadeStartDistance;
    }

    private void CreateDefaultFlashlight()
    {
        GameObject lightObj = new GameObject("Spot Light");
        lightObj.transform.parent = cameraTransform;
        lightObj.transform.localPosition = new Vector3(0f, 0f, 0.2f);
        lightObj.transform.localRotation = Quaternion.identity;

        flashlight = lightObj.AddComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.range = 25f;
        flashlight.spotAngle = 55f;
        flashlight.intensity = 3.5f;
        flashlight.shadows = LightShadows.Soft;
        baseIntensity = flashlight.intensity;
        baseSpotAngle = flashlight.spotAngle;
        baseLocalPosition = flashlight.transform.localPosition;
        
        Debug.Log("<color=green>[FlashlightController]</color> Created default Spot Light under Main Camera.");
    }
}
