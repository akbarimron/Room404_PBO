using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private string deathSceneName = "deathScene";

    [Header("Death Count Settings")]
    [Tooltip("Jumlah kematian sebelum player dipaksa keluar ke scene luar (OUTSIDE KOS).")]
    [SerializeField] private int maxDeathsBeforeEscape = 3;
    [Tooltip("Nama scene yang akan di-load setelah mati sebanyak maxDeathsBeforeEscape kali.")]
    [SerializeField] private string escapeSceneName = "OUTSIDE KOS";

    // Persistent death counter — bertahan antar respawn selama sesi game
    private static int totalDeathCount = 0;

    [Header("UI Reference")]
    public Image[] hearts;

    [Header("Dramatic Effects Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.2f;
    private CameraShake cameraShake;

    private CharacterController controller;
    private PlayerMovement playerMovement;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        cameraShake = GetComponentInChildren<CameraShake>();

        // Cari UI Hearts secara dinamis jika kosong
        if (hearts == null || hearts.Length == 0)
        {
            var list = new System.Collections.Generic.List<Image>();
            Image[] allImages = Resources.FindObjectsOfTypeAll<Image>();
            foreach (var img in allImages)
            {
                if (img != null && img.gameObject != null && img.gameObject.scene.isLoaded &&
                    (img.gameObject.name.ToLower().Contains("heart") || img.gameObject.name.ToLower().Contains("nyawa")))
                {
                    list.Add(img);
                }
            }
            if (list.Count > 0)
            {
                list.Sort((a, b) => string.Compare(a.name, b.name));
                hearts = list.ToArray();
                Debug.Log($"<color=green>[PlayerHealth]</color> Auto-bound {hearts.Length} hearts: " + string.Join(", ", list.ConvertAll(h => h.name)));
            }
        }

        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        Die();
    }

    void UpdateHealthUI()
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
                hearts[i].enabled = (i < currentHealth);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Tambah hitungan kematian
        totalDeathCount++;
        Debug.Log($"<color=red>[PlayerHealth]</color> Player mati! Total kematian: {totalDeathCount}/{maxDeathsBeforeEscape}");

        // Slow motion saat mati
        Time.timeScale = 0.3f;

        if (cameraShake != null)
            cameraShake.Shake(shakeDuration, shakeMagnitude);

        // Matikan kontrol pergerakan
        if (controller != null) controller.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;

        // Tampilkan death scene overlay
        SceneManager.LoadScene(deathSceneName, LoadSceneMode.Additive);

        // Cek apakah sudah mencapai batas kematian
        if (totalDeathCount >= maxDeathsBeforeEscape)
        {
            StartCoroutine(EscapeProcess());
        }
        else
        {
            StartCoroutine(RespawnProcess());
        }
    }

    // ─── Respawn biasa (kematian ke-1 dan ke-2) ───────────────────────────
    IEnumerator RespawnProcess()
    {
        yield return new WaitForSecondsRealtime(4f);

        Time.timeScale = 1.0f;
        SceneManager.UnloadSceneAsync(deathSceneName);

        // Kembali ke posisi spawn
        transform.position = spawnPoint;
        yield return null;

        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();

        if (controller != null) controller.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;

        Debug.Log($"<color=green>[PlayerHealth]</color> Player respawn! ({totalDeathCount}/{maxDeathsBeforeEscape} kematian)");
    }

    // ─── Escape ke OUTSIDE KOS (setelah 3 kali mati) ─────────────────────
    IEnumerator EscapeProcess()
    {
        Debug.Log($"<color=orange>[PlayerHealth]</color> Player sudah mati {totalDeathCount}x — pindah ke scene '{escapeSceneName}'!");

        // Beri waktu death scene ditampilkan dulu
        yield return new WaitForSecondsRealtime(4f);

        // Reset timeScale sebelum pindah scene
        Time.timeScale = 1.0f;

        // Reset hitungan mati untuk sesi berikutnya
        totalDeathCount = 0;

        // Unload death overlay scene
        if (SceneManager.GetSceneByName(deathSceneName).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(deathSceneName);
        }

        // Load OUTSIDE KOS secara langsung (bukan additive)
        SceneManager.LoadScene(escapeSceneName, LoadSceneMode.Single);
    }

    // ─── Publik untuk reset manual (misal dari menu / cheat) ─────────────
    public static void ResetDeathCount()
    {
        totalDeathCount = 0;
        Debug.Log("[PlayerHealth] Death count di-reset.");
    }

    public static int GetDeathCount() => totalDeathCount;
}