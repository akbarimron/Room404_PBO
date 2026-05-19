using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Quaternion originalRotation;
    private Vector3 originalPosition;

    void OnEnable()
    {
        originalRotation = transform.localRotation;
        originalPosition = transform.localPosition;
    }

    // Fungsi ini sekarang akan membuat kamera terasa pusing/oleng halus
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ProcessDizzy(duration, magnitude));
    }

    private IEnumerator ProcessDizzy(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        // Menggunakan kebalikan dari magnitude untuk membuat ayunan yang halus (sinusoidal)
        while (elapsed < duration)
        {
            // Membuat efek ayunan melingkar/bergelombang seperti orang pusing
            float tiltZ = Mathf.Sin(elapsed * 5f) * magnitude * 15f; // Miring kanan-kiri
            float tiltX = Mathf.Cos(elapsed * 3f) * magnitude * 5f;  // Mengangguk atas-bawah

            // Terapkan rotasi baru secara halus
            transform.localRotation = originalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

            // Sedikit efek limbung pada posisi (opsional, sangat halus)
            float swayX = Mathf.Sin(elapsed * 2f) * (magnitude * 0.1f);
            transform.localPosition = originalPosition + new Vector3(swayX, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Kembalikan posisi dan rotasi kamera ke semula secara perlahan
        float resetElapsed = 0f;
        Quaternion currentRot = transform.localRotation;
        Vector3 currentPos = transform.localPosition;

        while (resetElapsed < 0.5f)
        {
            resetElapsed += Time.deltaTime;
            float t = resetElapsed / 0.5f;
            transform.localRotation = Quaternion.Slerp(currentRot, originalRotation, t);
            transform.localPosition = Vector3.Lerp(currentPos, originalPosition, t);
            yield return null;
        }
    }
}