using UnityEngine;

public class HandViewModel : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool parentToCamera = true;

    [Header("Hand Offset")]
    [SerializeField] private Vector3 localPositionOffset = new Vector3(0.28f, -0.3f, 0.5f);
    [SerializeField] private Vector3 localRotationOffset = new Vector3(6f, -18f, 0f);
    [SerializeField] private Vector3 localScale = Vector3.one;

    [Header("Follow")]
    [SerializeField] private bool smoothFollow = false;
    [SerializeField] private float followSpeed = 20f;

    [Header("Right Hand Only")]
    [SerializeField] private bool hideLeftHandByName = true;
    [SerializeField] private bool keepRightmostRendererOnly = false;

    private readonly string[] leftHandNameTokens = { "left", "l_", "_l", "kiri" };

    private void Start()
    {
        ResolveCamera();

        if (cameraTransform == null)
        {
            Debug.LogWarning("HandViewModel: Main Camera not found. Assign cameraTransform in Inspector.");
            enabled = false;
            return;
        }

        if (parentToCamera)
        {
            transform.SetParent(cameraTransform, false);
            ApplyLocalPose();
        }

        FilterLeftHand();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        if (parentToCamera)
        {
            if (smoothFollow)
            {
                Vector3 targetPos = localPositionOffset;
                Quaternion targetRot = Quaternion.Euler(localRotationOffset);

                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, followSpeed * Time.deltaTime);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, followSpeed * Time.deltaTime);
            }
            else
            {
                ApplyLocalPose();
            }

            transform.localScale = localScale;
            return;
        }

        Vector3 worldPos = cameraTransform.TransformPoint(localPositionOffset);
        Quaternion worldRot = cameraTransform.rotation * Quaternion.Euler(localRotationOffset);

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, worldPos, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, worldRot, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.SetPositionAndRotation(worldPos, worldRot);
        }

        transform.localScale = localScale;
    }

    private void ResolveCamera()
    {
        if (cameraTransform != null)
        {
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
        }
    }

    private void ApplyLocalPose()
    {
        transform.localPosition = localPositionOffset;
        transform.localRotation = Quaternion.Euler(localRotationOffset);
        transform.localScale = localScale;
    }

    private void FilterLeftHand()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        if (hideLeftHandByName)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                string lowerName = renderers[i].gameObject.name.ToLowerInvariant();
                for (int j = 0; j < leftHandNameTokens.Length; j++)
                {
                    if (lowerName.Contains(leftHandNameTokens[j]))
                    {
                        renderers[i].enabled = false;
                        break;
                    }
                }
            }
        }

        if (!keepRightmostRendererOnly)
        {
            return;
        }

        Renderer rightmost = null;
        float maxX = float.NegativeInfinity;

        for (int i = 0; i < renderers.Length; i++)
        {
            float x = transform.InverseTransformPoint(renderers[i].bounds.center).x;
            if (x > maxX)
            {
                maxX = x;
                rightmost = renderers[i];
            }
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = renderers[i] == rightmost;
        }
    }
}
