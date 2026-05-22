using UnityEngine;

public class StairRampCollider : MonoBehaviour
{
    [Header("Ramp Collider")]
    [SerializeField] private Vector3 localCenter = Vector3.zero;
    [SerializeField] private Vector3 localEulerAngles = new Vector3(30f, 0f, 0f);
    [SerializeField] private Vector3 localSize = new Vector3(2f, 0.2f, 4f);
    [SerializeField] private bool isTrigger = false;

    private const string RampColliderName = "StairRampCollider";

    void Reset()
    {
        FitToRendererBounds();
    }

    [ContextMenu("Fit To Renderer Bounds")]
    public void FitToRendererBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        localCenter = transform.InverseTransformPoint(bounds.center);
        localSize = new Vector3(
            Mathf.Max(0.1f, bounds.size.x / Mathf.Max(0.001f, transform.lossyScale.x)),
            0.2f,
            Mathf.Max(0.1f, bounds.size.z / Mathf.Max(0.001f, transform.lossyScale.z))
        );
    }

    [ContextMenu("Create Or Update Ramp Collider")]
    public void CreateOrUpdateRampCollider()
    {
        Transform rampTransform = transform.Find(RampColliderName);
        if (rampTransform == null)
        {
            GameObject rampObject = new GameObject(RampColliderName);
            rampTransform = rampObject.transform;
            rampTransform.SetParent(transform, false);
        }

        rampTransform.localPosition = localCenter;
        rampTransform.localRotation = Quaternion.Euler(localEulerAngles);
        rampTransform.localScale = Vector3.one;

        BoxCollider boxCollider = rampTransform.GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = rampTransform.gameObject.AddComponent<BoxCollider>();

        boxCollider.center = Vector3.zero;
        boxCollider.size = localSize;
        boxCollider.isTrigger = isTrigger;
    }
}
