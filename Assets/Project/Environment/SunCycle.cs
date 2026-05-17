using UnityEngine;

[ExecuteAlways]
public class SunCycle : MonoBehaviour
{
    [Tooltip("Directional Light used as Sun")]
    public Light sun;

    [Tooltip("Duration of full day in seconds")]
    public float dayDuration = 120f; // 2 minutes per full rotation

    [Tooltip("Gradient color of sunlight over normalized day (0..1)")]
    public Gradient sunColorOverDay;

    [Tooltip("Intensity curve over normalized day (0..1)")]
    public AnimationCurve intensityOverDay = AnimationCurve.EaseInOut(0, 0.2f, 1, 1f);

    [Range(0f,1f)]
    public float timeOfDay = 0f; // 0..1, editable in inspector for testing

    void Start()
    {
        if (sun == null)
        {
            var dir = FindObjectOfType<Light>();
            if (dir != null && dir.type == LightType.Directional)
                sun = dir;
        }
    }

    void Update()
    {
        if (sun == null)
            return;

        // Advance time in Play mode
        if (Application.isPlaying && dayDuration > 0f)
        {
            timeOfDay += Time.deltaTime / dayDuration;
            if (timeOfDay > 1f) timeOfDay -= 1f;
        }

        // Rotate sun: 0..1 maps to -90 (midnight) .. 270 (next midnight) so 0.25 = sunrise, 0.75 = sunset
        float sunAngle = Mathf.Lerp(-90f, 270f, timeOfDay);
        sun.transform.rotation = Quaternion.Euler(new Vector3(sunAngle, 170f, 0f));

        // Update color and intensity
        sun.color = sunColorOverDay.Evaluate(timeOfDay);
        sun.intensity = intensityOverDay.Evaluate(timeOfDay);

        // Optional: adjust ambient light to match sun
        RenderSettings.ambientLight = sun.color * Mathf.Clamp01(sun.intensity * 0.6f);
    }
}
