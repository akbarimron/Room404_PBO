using UnityEngine;
using TMPro; // Gunakan ini jika teks kamu menggunakan TextMeshPro
// using UnityEngine.UI; // Hapus tanda komentar '//' di kiri jika menggunakan Text biasa bawaan Unity

public class InteractionUI : MonoBehaviour
{
    private static InteractionUI _instance;

    private static void CleanupDuplicates()
    {
        InteractionUI[] all = FindObjectsByType<InteractionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all.Length <= 1) return;

        // Prefer keeping the original UI in the scene, not the dynamically created "InteractionUI_Canvas"
        InteractionUI best = null;
        foreach (var ui in all)
        {
            if (ui != null && ui.gameObject.name != "InteractionUI_Canvas")
            {
                best = ui;
                break;
            }
        }

        if (best == null)
        {
            foreach (var ui in all)
            {
                if (ui != null)
                {
                    best = ui;
                    break;
                }
            }
        }

        if (best != null)
        {
            _instance = best;
            foreach (var ui in all)
            {
                if (ui != null && ui != best)
                {
                    Debug.LogWarning($"[InteractionUI] Destroying duplicate InteractionUI component on GameObject: {ui.gameObject.name}");
                    Destroy(ui.gameObject);
                }
            }
        }
    }

    public static InteractionUI Instance
    {
        get
        {
            CleanupDuplicates();

            if (_instance == null)
            {
                InteractionUI[] all = FindObjectsByType<InteractionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var ui in all)
                {
                    if (ui != null)
                    {
                        _instance = ui;
                        break;
                    }
                }

                if (_instance == null)
                {
                    _instance = CreateDynamicFallbackUI();
                }
            }

            if (_instance != null && _instance.gameObject != null && !_instance.gameObject.activeSelf)
            {
                _instance.gameObject.SetActive(true);
            }

            return _instance;
        }
    }

    [Header("UI Component")]
    [SerializeField] private TMP_Text interactionText;

    [Header("UI Position Settings")]
    [Tooltip("Anchor Y position (0 = bottom, 1 = top) for the interaction text.")]
    [SerializeField] private float textAnchorY = 0.15f;

    void Awake()
    {
        CleanupDuplicates();
        if (_instance == null)
        {
            _instance = this;
        }
    }

    void Start()
    {
        Debug.Log("<color=green>[InteractionUI]</color> Script started on " + gameObject.name);
        EnsureInteractionText();
        HideText();
    }

    private void EnsureInteractionText()
    {
        if (interactionText == null)
        {
            // Coba cari di komponen anak (termasuk yang non-aktif) sebagai fallback
            interactionText = GetComponentInChildren<TMP_Text>(true);
            if (interactionText != null)
            {
                Debug.Log($"<color=green>[InteractionUI]</color> Berhasil memulihkan 'interactionText' dari child component: {interactionText.gameObject.name}");
            }
            else
            {
                // Jika benar-benar tidak ditemukan, kita buat secara dinamis sebagai child agar self-heal
                Debug.LogWarning("<color=orange>[InteractionUI]</color> 'interactionText' tidak di-assign di Inspector dan tidak ditemukan di children! Membuat komponen 'InteractionText_Dynamic' baru secara otomatis.");
                
                GameObject textObj = new GameObject("InteractionText_Dynamic");
                textObj.transform.SetParent(transform, false);

                TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
                interactionText = tmpText;
            }
        }

        // Sekarang kita selalu pastikan style & posisinya konsisten di runtime
        if (interactionText != null)
        {
            interactionText.color = Color.red;
            interactionText.alignment = TextAlignmentOptions.Center;
            
            // Tambahkan outline agar teks terbaca jelas di berbagai background game
            interactionText.outlineColor = Color.black;
            interactionText.outlineWidth = 0.2f;

            RectTransform rectTransform = interactionText.rectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, textAnchorY);
                rectTransform.anchorMax = new Vector2(0.5f, textAnchorY);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(1000, 100);
            }
        }
    }

    // Fungsi untuk menampilkan teks
    public void ShowText(string message)
    {
        EnsureInteractionText();
        if (interactionText != null)
        {
            interactionText.text = message;
            interactionText.gameObject.SetActive(true);
        }
    }

    // Fungsi untuk menyembunyikan teks
    public void HideText()
    {
        EnsureInteractionText();
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    private static InteractionUI CreateDynamicFallbackUI()
    {
        Debug.LogWarning("<color=orange>[InteractionUI]</color> No InteractionUI found in the scene! Creating a dynamic fallback UI Canvas.");

        // Create Canvas GameObject
        GameObject canvasObj = new GameObject("InteractionUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Create Text GameObject
        GameObject textObj = new GameObject("InteractionText");
        textObj.transform.SetParent(canvasObj.transform, false);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 28;
        tmpText.color = Color.red;
        
        tmpText.outlineColor = Color.black;
        tmpText.outlineWidth = 0.2f;

        RectTransform rectTransform = tmpText.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.15f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.15f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(1000, 100);

        // Add InteractionUI component
        InteractionUI newUI = canvasObj.AddComponent<InteractionUI>();
        newUI.textAnchorY = 0.15f;
        newUI.interactionText = tmpText;

        DontDestroyOnLoad(canvasObj);

        return newUI;
    }
}