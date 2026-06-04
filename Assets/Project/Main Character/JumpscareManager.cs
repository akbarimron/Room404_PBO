using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// JumpscareManager — Mengatur seluruh alur jumpscare saat hantu menangkap player.
///
/// ALUR:
///   1. Hantu jadi invisible (hilang dari pandangan, diam di tempat)
///   2. Flash putih menyambar layar
///   3. Wajah hantu zoom-in ke layar + suara jumpscare
///   4. Nyawa player dikurangi 1
///   5. Hantu diteleport jauh (lantai 3-4)
///   6. Wajah fade-out
///   7. Layar kelap-kelip (flicker hitam) sebelum normal
///   8. Player kembali bisa bergerak
///
/// SETUP DI UNITY:
///   1. Buat GameObject "JumpscareManager", tambahkan script ini.
///   2. Buat Canvas: Render Mode = Screen Space Overlay, Sort Order = 999.
///      - Child Image "FlashOverlay": stretch full, warna putih, alpha 0.
///      - Child Image "GhostFaceImage": anchor center-middle, assign Sprite wajah hantu, alpha 0.
///   3. Assign Flash Overlay, Ghost Face Image di Inspector.
///   4. (Opsional) Assign Jumpscare Audio Source dengan clip jumpscare.mp3.
/// </summary>
public class JumpscareManager : MonoBehaviour
{
    public static JumpscareManager Instance { get; private set; }

    // ─── Canvas References ────────────────────────────────────────────────────
    [Header("Canvas References")]
    [Tooltip("Image putih yang menutupi layar (flash & flicker). Stretch full screen.")]
    [SerializeField] private Image flashOverlay;

    [Tooltip("Image wajah hantu di tengah layar. Assign Sprite-nya.")]
    [SerializeField] private Image ghostFaceImage;

    // ─── Timing ───────────────────────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("Durasi flash putih awal (detik). Makin kecil = makin mengejutkan.")]
    [SerializeField] private float flashInDuration = 0.06f;

    [Tooltip("Berapa lama wajah hantu tampil di layar sebelum fade out.")]
    [SerializeField] private float faceDuration = 0.9f;

    [Tooltip("Durasi fade-out wajah hantu.")]
    [SerializeField] private float faceOutDuration = 0.35f;

    // ─── Flicker ─────────────────────────────────────────────────────────────
    [Header("Flicker Effect (Kelap-Kelip)")]
    [Tooltip("Jumlah kelipan layar setelah jumpscare.")]
    [SerializeField] private int flickerCount = 6;

    [Tooltip("Durasi setiap satu kelipan (detik).")]
    [SerializeField] private float flickerInterval = 0.09f;

    [Tooltip("Intensitas kelipan: 0=tidak terlihat, 1=layar penuh hitam.")]
    [Range(0f, 1f)]
    [SerializeField] private float flickerAlpha = 0.85f;

    [Tooltip("Warna kelipan layar (default hitam).")]
    [SerializeField] private Color flickerColor = Color.black;

    // ─── Ghost Face Scale ─────────────────────────────────────────────────────
    [Header("Ghost Face Scale")]
    [Tooltip("Skala awal wajah hantu (zoom dari kecil ke besar).")]
    [SerializeField] private float faceStartScale = 0.5f;

    [Tooltip("Skala akhir wajah hantu saat terlihat penuh.")]
    [SerializeField] private float faceEndScale = 1.1f;

    // ─── Audio ────────────────────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("AudioSource 2D untuk suara jumpscare. Akan auto-load jumpscare.mp3 jika clip kosong.")]
    [SerializeField] private AudioSource jumpscareAudio;

    // ─── Player Freeze ────────────────────────────────────────────────────────
    [Header("Player Freeze")]
    [Tooltip("Total durasi player difreeze sejak jumpscare dimulai (detik).")]
    [SerializeField] private float totalFreezeDuration = 3.5f;

    // ─── Internal State ───────────────────────────────────────────────────────
    private bool isJumpscaring = false;
    private MonoBehaviour cachedPlayerMovement;
    private MonoBehaviour cachedMouseLook;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Sembunyikan UI saat mulai
        HideUI();

        // Setup AudioSource jika belum diassign
        SetupAudio();

        // Cache player components
        CachePlayerComponents();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Panggil ini dari EnemyDamage saat hantu menyentuh player.
    /// ghost = referensi GhostAI yang menangkap player.
    /// playerHealth = referensi PlayerHealth untuk kurangi nyawa.
    /// </summary>
    public void TriggerJumpscare(GhostAI ghost, PlayerHealth playerHealth)
    {
        if (isJumpscaring) return;
        StartCoroutine(JumpscareSequence(ghost, playerHealth));
    }

    /// <summary>
    /// Overload tanpa parameter (fallback / test dari Inspector).
    /// </summary>
    public void TriggerJumpscare()
    {
        if (isJumpscaring) return;
        StartCoroutine(JumpscareSequence(null, null));
    }

    [ContextMenu("Test Jumpscare")]
    public void TestJumpscare() => TriggerJumpscare();

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Jumpscare Sequence

    private IEnumerator JumpscareSequence(GhostAI ghost, PlayerHealth playerHealth)
    {
        isJumpscaring = true;

        // ── Langkah 1: Freeze player ──────────────────────────────────────
        SetPlayerFrozen(true);

        // ── Langkah 2: Hantu jadi invisible & diam ────────────────────────
        if (ghost != null)
        {
            ghost.SetInvisible(true);
        }

        // ── Langkah 3: Mainkan suara jumpscare ────────────────────────────
        PlayJumpscareAudio();

        // ── Langkah 4: Flash putih menyambar layar ────────────────────────
        if (flashOverlay != null)
        {
            flashOverlay.color = Color.white;
            flashOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(FadeImage(flashOverlay, 0f, 1f, flashInDuration));
        }

        // ── Langkah 5: Tampilkan wajah hantu dengan zoom-in ──────────────
        if (ghostFaceImage != null)
        {
            ghostFaceImage.gameObject.SetActive(true);
            ghostFaceImage.transform.localScale = Vector3.one * faceStartScale;
            SetAlpha(ghostFaceImage, 0f);

            float elapsed = 0f;
            float zoomDur = flashInDuration * 1.5f;
            while (elapsed < zoomDur)
            {
                float t = elapsed / zoomDur;
                float easedT = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic
                SetAlpha(ghostFaceImage, easedT);
                ghostFaceImage.transform.localScale = Vector3.one * Mathf.Lerp(faceStartScale, faceEndScale, easedT);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetAlpha(ghostFaceImage, 1f);
            ghostFaceImage.transform.localScale = Vector3.one * faceEndScale;
        }

        // Fade flash overlay sedikit supaya wajah terlihat
        if (flashOverlay != null)
        {
            yield return StartCoroutine(FadeImage(flashOverlay, 1f, 0.1f, 0.08f));
        }

        // ── Langkah 6: Kurangi nyawa player ──────────────────────────────
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
        }

        // ── Langkah 7: Teleport hantu jauh ke lantai atas ────────────────
        if (ghost != null)
        {
            ghost.TeleportFarFromPlayer();
        }

        // ── Langkah 8: Tahan wajah selama faceDuration ───────────────────
        yield return new WaitForSecondsRealtime(faceDuration);

        // ── Langkah 9: Fade out wajah hantu ──────────────────────────────
        if (ghostFaceImage != null)
        {
            yield return StartCoroutine(FadeImage(ghostFaceImage, 1f, 0f, faceOutDuration));
            ghostFaceImage.gameObject.SetActive(false);
        }

        if (flashOverlay != null)
        {
            yield return StartCoroutine(FadeImage(flashOverlay, 0.1f, 0f, faceOutDuration * 0.5f));
            flashOverlay.gameObject.SetActive(false);
        }

        // ── Langkah 10: Hantu visible kembali (di posisi baru) ────────────
        if (ghost != null)
        {
            ghost.SetInvisible(false);
        }

        // ── Langkah 11: Efek kelap-kelip (flicker) sebelum normal ─────────
        yield return StartCoroutine(PlayFlicker());

        // ── Langkah 12: Unfreeze player ───────────────────────────────────
        // Hitung sisa freeze jika total freeze belum habis
        // (sebagian besar freeze sudah berlalu selama jumpscare)
        SetPlayerFrozen(false);

        isJumpscaring = false;
        Debug.Log("[JumpscareManager] Jumpscare sequence selesai.");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Flicker Effect

    /// <summary>
    /// Efek kelap-kelip layar hitam setelah jumpscare selesai,
    /// seolah-olah penglihatan player baru pulih dari teror.
    /// </summary>
    private IEnumerator PlayFlicker()
    {
        if (flashOverlay == null) yield break;

        flashOverlay.color = flickerColor;
        flashOverlay.gameObject.SetActive(true);

        // Intensitas kelipan berkurang dari kuat ke lemah (semakin memudar)
        for (int i = 0; i < flickerCount; i++)
        {
            // Intensitas makin lemah seiring kelipan berlanjut
            float progress = 1f - ((float)i / flickerCount);
            float targetAlpha = flickerAlpha * progress;

            // Nyalakan
            SetAlpha(flashOverlay, targetAlpha);
            yield return new WaitForSecondsRealtime(flickerInterval * 0.4f);

            // Matikan
            SetAlpha(flashOverlay, 0f);
            yield return new WaitForSecondsRealtime(flickerInterval * 0.6f);
        }

        // Pastikan overlay benar-benar tersembunyi di akhir
        SetAlpha(flashOverlay, 0f);
        flashOverlay.gameObject.SetActive(false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Player Freeze / Unfreeze

    private void SetPlayerFrozen(bool frozen)
    {
        if (cachedPlayerMovement == null || cachedMouseLook == null)
            CachePlayerComponents();

        if (cachedPlayerMovement != null)
            cachedPlayerMovement.enabled = !frozen;

        if (cachedMouseLook != null)
            cachedMouseLook.enabled = !frozen;
    }

    private void CachePlayerComponents()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }

        if (playerObj == null) return;

        cachedPlayerMovement = playerObj.GetComponent<PlayerMovement>();

        // Cari mouselook (script di-attach langsung ke Player atau Camera child)
        cachedMouseLook = playerObj.GetComponent<mouselook>();
        if (cachedMouseLook == null)
            cachedMouseLook = playerObj.GetComponentInChildren<mouselook>();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Audio

    private void PlayJumpscareAudio()
    {
        if (jumpscareAudio == null) return;
        jumpscareAudio.volume = 1f;
        jumpscareAudio.Stop();
        jumpscareAudio.Play();
    }

    private void SetupAudio()
    {
        if (jumpscareAudio == null)
        {
            jumpscareAudio = GetComponent<AudioSource>();
            if (jumpscareAudio == null)
                jumpscareAudio = gameObject.AddComponent<AudioSource>();
        }

        jumpscareAudio.spatialBlend = 0f;   // 2D — terdengar sama dari mana pun
        jumpscareAudio.playOnAwake = false;
        jumpscareAudio.loop = false;

#if UNITY_EDITOR
        if (jumpscareAudio.clip == null)
        {
            jumpscareAudio.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/NewAssets/SFX/jumpscare.mp3");
            if (jumpscareAudio.clip == null)
                Debug.LogWarning("[JumpscareManager] jumpscare.mp3 tidak ditemukan di Assets/NewAssets/SFX/. Assign manual di Inspector.");
        }
#endif
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Helpers

    private void HideUI()
    {
        if (flashOverlay != null)
        {
            SetAlpha(flashOverlay, 0f);
            flashOverlay.gameObject.SetActive(false);
        }
        if (ghostFaceImage != null)
        {
            SetAlpha(ghostFaceImage, 0f);
            ghostFaceImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
    {
        if (image == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetAlpha(image, Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        SetAlpha(image, toAlpha);
    }

    private void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    #endregion
}
