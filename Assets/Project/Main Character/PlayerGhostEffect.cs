using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerGhostEffect : MonoBehaviour
{
    [Header("Distance Settings")]
    [SerializeField] private float effectDistance = 12f; // Jarak mulai efek

    [Header("Camera Shake Settings")]
    [SerializeField] private float maxShakePosition = 0.04f; // Goyang posisi maks
    [SerializeField] private float maxShakeRotation = 1.2f;   // Goyang rotasi maks

    [Header("FOV Distortion Settings")]
    [SerializeField] private float maxFovJitter = 5f;       // Jitter FOV maks (distorsi zoom)
    [SerializeField] private float distortionSpeed = 12f;    // Kecepatan denyut distorsi

    [Header("Shader Fallback Settings")]
    [SerializeField] private Shader desaturationShader;
    [Range(0f, 1f)] [SerializeField] private float targetDesaturation = 0.85f;
    [Range(-1f, 1f)] [SerializeField] private float targetDistortion = -0.35f;

    private Camera mainCamera;
    private GhostAI ghost;
    
    private float originalFov;
    private bool hasOriginals = false;

    // Post Processing URP Volume
    private Volume volume;
    private ColorAdjustments colorAdjustments;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;

    // Fallback Canvas & Material
    private GameObject fallbackCanvasObj;
    private Material fallbackMaterial;

    // State for temporary camera shake restoration
    private Vector3 preShakePos;
    private Quaternion preShakeRot;
    private bool didShakeLastFrame = false;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        SaveOriginals();
        SetupPostProcessing();
    }

    private void SaveOriginals()
    {
        if (mainCamera != null && !hasOriginals)
        {
            originalFov = mainCamera.fieldOfView;
            hasOriginals = true;
        }
    }

    private void SetupPostProcessing()
    {
        if (mainCamera == null) return;

        // 1. Try to set up standard URP Volume post processing
        try
        {
            var cameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = true;
            }

            // Create custom Volume GameObject
            GameObject volumeObj = new GameObject("GhostEffectVolume");
            volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100;
            volume.weight = 0f;

            // Setup layer based on Camera's Volume Mask to ensure it renders
            if (cameraData != null)
            {
                int mask = cameraData.volumeLayerMask.value;
                int targetLayer = 0;
                for (int i = 0; i < 32; i++)
                {
                    if (((mask >> i) & 1) == 1)
                    {
                        targetLayer = i;
                        break;
                    }
                }
                volumeObj.layer = targetLayer;
            }

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "GhostEffectProfile";

            colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 0f;

            lensDistortion = profile.Add<LensDistortion>(true);
            lensDistortion.intensity.overrideState = true;
            lensDistortion.intensity.value = 0f;

            chromaticAberration = profile.Add<ChromaticAberration>(true);
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = 0f;

            volume.profile = profile;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to setup URP Volume, relying on Canvas fallback. Error: " + ex.Message);
        }

        // 2. Setup Canvas fallback with custom shader
        SetupCanvasFallback();
    }

    private void SetupCanvasFallback()
    {
        if (mainCamera == null) return;

        // Force enable opaque texture support on the URP asset
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
        {
            urpAsset.supportsCameraOpaqueTexture = true;
        }

        Shader targetShader = desaturationShader;
        if (targetShader == null)
        {
            targetShader = Shader.Find("Custom/ScreenDesaturation");
        }

        if (targetShader == null)
        {
            Debug.LogWarning("Shader 'Custom/ScreenDesaturation' not found! Canvas fallback will not work.");
            return;
        }

        try
        {
            fallbackMaterial = new Material(targetShader);
            fallbackMaterial.SetFloat("_Intensity", 0f);
            fallbackMaterial.SetFloat("_Desaturation", targetDesaturation);
            fallbackMaterial.SetFloat("_Distortion", targetDistortion);

            fallbackCanvasObj = new GameObject("GhostEffectCanvasFallback");
            fallbackCanvasObj.layer = 5; // UI layer
            Canvas canvas = fallbackCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            canvas.planeDistance = mainCamera.nearClipPlane + 0.01f;
            canvas.sortingOrder = 999;

            UnityEngine.UI.CanvasScaler scaler = fallbackCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            GameObject rawImageObj = new GameObject("EffectRawImage");
            rawImageObj.transform.SetParent(fallbackCanvasObj.transform, false);
            rawImageObj.layer = 5; // UI Layer
            UnityEngine.UI.RawImage rawImage = rawImageObj.AddComponent<UnityEngine.UI.RawImage>();
            rawImage.material = fallbackMaterial;

            // Stretch to cover whole screen
            RectTransform rect = rawImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to setup Canvas Fallback. Error: " + ex.Message);
        }
    }

    void Update()
    {
        // Restore camera position and rotation from previous frame shake
        if (didShakeLastFrame && mainCamera != null)
        {
            mainCamera.transform.localPosition = preShakePos;
            mainCamera.transform.localRotation = preShakeRot;
            didShakeLastFrame = false;
        }

        if (mainCamera == null) return;

        // Find ghost if not cached
        if (ghost == null)
        {
            ghost = Object.FindFirstObjectByType<GhostAI>();
        }

        if (ghost == null)
        {
            ResetCameraEffectsSmooth();
            return;
        }

        float distance = Vector3.Distance(transform.position, ghost.transform.position);

        if (distance < effectDistance)
        {
            SaveOriginals();

            // Calculate effect intensity (0.0 at edge, 1.0 when touching)
            float intensity = 1f - (distance / effectDistance);

            // 1. Update URP Post Processing Volume
            if (volume != null)
            {
                volume.weight = intensity;

                if (colorAdjustments != null)
                {
                    colorAdjustments.saturation.value = Mathf.Lerp(0f, -85f, intensity);
                }

                if (lensDistortion != null)
                {
                    lensDistortion.intensity.value = Mathf.Lerp(0f, targetDistortion, intensity);
                }

                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.value = Mathf.Lerp(0f, 0.8f, intensity);
                }
            }

            // 2. Update Fallback Canvas Material
            if (fallbackMaterial != null)
            {
                fallbackMaterial.SetFloat("_Intensity", intensity);
                fallbackMaterial.SetFloat("_Desaturation", targetDesaturation);
                fallbackMaterial.SetFloat("_Distortion", targetDistortion);
            }
        }
        else
        {
            ResetCameraEffectsSmooth();
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null || ghost == null) return;

        float distance = Vector3.Distance(transform.position, ghost.transform.position);

        if (distance < effectDistance)
        {
            float intensity = 1f - (distance / effectDistance);

            // Save clean camera position & rotation (includes player movement, mouse look, bobbing)
            preShakePos = mainCamera.transform.localPosition;
            preShakeRot = mainCamera.transform.localRotation;
            didShakeLastFrame = true;

            // Apply camera translation shake
            Vector3 randomPosOffset = new Vector3(
                Random.Range(-1f, 1f) * maxShakePosition * intensity,
                Random.Range(-1f, 1f) * maxShakePosition * intensity,
                Random.Range(-1f, 1f) * maxShakePosition * intensity
            );
            mainCamera.transform.localPosition = preShakePos + randomPosOffset;

            // Apply camera rotation shake
            Vector3 randomRotOffset = new Vector3(
                Random.Range(-1f, 1f) * maxShakeRotation * intensity,
                Random.Range(-1f, 1f) * maxShakeRotation * intensity,
                Random.Range(-1f, 1f) * maxShakeRotation * intensity
            );
            mainCamera.transform.localRotation = preShakeRot * Quaternion.Euler(randomRotOffset);

            // Apply camera FOV jitter
            float pulse = Mathf.Sin(Time.time * distortionSpeed) * maxFovJitter * intensity;
            mainCamera.fieldOfView = originalFov + pulse + Random.Range(-0.5f, 0.5f) * intensity;
        }
    }

    private void ResetCameraEffectsSmooth()
    {
        if (volume != null && volume.weight > 0f)
        {
            volume.weight = Mathf.Lerp(volume.weight, 0f, Time.deltaTime * 5f);
            if (volume.weight < 0.01f)
            {
                volume.weight = 0f;
            }
        }

        if (fallbackMaterial != null)
        {
            float curIntensity = fallbackMaterial.GetFloat("_Intensity");
            if (curIntensity > 0f)
            {
                curIntensity = Mathf.Lerp(curIntensity, 0f, Time.deltaTime * 5f);
                if (curIntensity < 0.01f)
                {
                    curIntensity = 0f;
                }
                fallbackMaterial.SetFloat("_Intensity", curIntensity);
            }
        }

        if (!hasOriginals) return;

        // Smoothly restore FOV
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, originalFov, Time.deltaTime * 5f);
        if (Mathf.Abs(mainCamera.fieldOfView - originalFov) < 0.05f)
        {
            mainCamera.fieldOfView = originalFov;
        }
    }

    void OnDestroy()
    {
        // Clean up runtime created resources
        if (volume != null)
        {
            Destroy(volume.gameObject);
        }
        if (fallbackCanvasObj != null)
        {
            Destroy(fallbackCanvasObj);
        }
        if (fallbackMaterial != null)
        {
            Destroy(fallbackMaterial);
        }
    }
}
