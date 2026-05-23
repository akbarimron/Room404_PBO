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
        private set
        {
            _instance = value;
        }
    }

    [Header("UI Component")]
    [SerializeField] private TMP_Text interactionText; // Ganti jadi 'public Text interactionText' jika pakai teks biasa

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            if (_instance.gameObject != null)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }
    }

    void Start()
    {
        Debug.Log("<color=green>[InteractionUI]</color> Script started on " + gameObject.name);
        if (interactionText == null)
        {
            Debug.LogError("<color=red>[InteractionUI]</color> TMP_Text component 'interactionText' is NOT assigned in the Inspector on " + gameObject.name + "! Please drag your text UI component into this field.");
        }
        else
        {
            interactionText.color = Color.red;
        }
        // Pastikan teks mati saat awal game
        HideText();
    }

    // Fungsi untuk menampilkan teks
    public void ShowText(string message)
    {
        if (interactionText != null)
        {
            interactionText.text = message;
            interactionText.gameObject.SetActive(true);

            // Print a debug message to help figure out why it might be invisible
            if (!interactionText.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"<color=yellow>[InteractionUI]</color> Set text to '{message}', but the text GameObject is INACTIVE in the hierarchy. Check if its parent Canvas or parent GameObject is disabled!");
            }
            else
            {
                Debug.Log($"<color=green>[InteractionUI]</color> Prompt visible: '{message}'");
            }
        }
        else
        {
            Debug.LogError("<color=red>[InteractionUI]</color> Cannot show text because 'interactionText' is null! Check Inspector settings on " + gameObject.name);
        }
    }

    // Fungsi untuk menyembunyikan teks
    public void HideText()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
            Debug.Log("<color=white>[InteractionUI]</color> Prompt hidden");
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