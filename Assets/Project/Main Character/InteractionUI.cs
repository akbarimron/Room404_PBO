using UnityEngine;
using TMPro; // Gunakan ini jika teks kamu menggunakan TextMeshPro
// using UnityEngine.UI; // Hapus tanda komentar '//' di kiri jika menggunakan Text biasa bawaan Unity

public class InteractionUI : MonoBehaviour
{
    private static InteractionUI _instance;
    public static InteractionUI Instance
    {
        get
        {
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

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            // Jika instance yang sudah ada adalah Canvas dinamis fallback, hancurkan fallback dan pakai Canvas asli ini
            if (_instance.gameObject != null && _instance.gameObject.name == "InteractionUI_Canvas")
            {
                Debug.Log("<color=green>[InteractionUI]</color> Menemukan UI fallback dinamis. Menghapusnya untuk menggunakan Canvas asli.");
                Destroy(_instance.gameObject);
                _instance = this;
            }
            else
            {
                // Jika duplikat UI asli lainnya, hancurkan duplikat baru ini
                Destroy(gameObject);
            }
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
        if (interactionText != null) return;

        // Coba cari di komponen anak (termasuk yang non-aktif) sebagai fallback
        interactionText = GetComponentInChildren<TMP_Text>(true);
        if (interactionText != null)
        {
            interactionText.color = Color.red;
            Debug.Log($"<color=green>[InteractionUI]</color> Berhasil memulihkan 'interactionText' dari child component: {interactionText.gameObject.name}");
        }
        else
        {
            Debug.LogError("<color=red>[InteractionUI]</color> 'interactionText' tidak di-assign di Inspector dan tidak ditemukan di children!");
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
        rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(1000, 100);

        // Add InteractionUI component
        InteractionUI newUI = canvasObj.AddComponent<InteractionUI>();
        newUI.interactionText = tmpText;

        DontDestroyOnLoad(canvasObj);

        return newUI;
    }
}