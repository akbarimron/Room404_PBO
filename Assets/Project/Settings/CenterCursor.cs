using UnityEngine;
using UnityEngine.UI;

// Locks the system cursor to center, hides it, and shows a small UI dot at screen center.
public class CenterCursor : MonoBehaviour
{
    [Tooltip("Enable the center dot UI")] public bool enableCenterDot = true;
    [Tooltip("Dot color (when no sprite is provided)")] public Color dotColor = Color.white;
    [Tooltip("Dot size in pixels")] public Vector2 dotSize = new Vector2(6f, 6f);
    [Tooltip("Optional sprite to use for the dot (overrides color)")] public Sprite dotSprite;
    [Tooltip("Optional Canvas to parent the dot under. If null, a top-level overlay canvas is created or reused.")]
    public Canvas parentCanvas;

    private GameObject dotGO;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!enableCenterDot)
            return;

        Canvas canvas = parentCanvas;
        if (canvas == null)
        {
            // Try to find an existing Screen Space - Overlay canvas
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    break;
                }
            }
        }

        if (canvas == null)
        {
            var canvasGO = new GameObject("CursorCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGO);
        }

        dotGO = new GameObject("CursorDot");
        dotGO.transform.SetParent(canvas.transform, false);
        var rt = dotGO.AddComponent<RectTransform>();
        rt.sizeDelta = dotSize;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var img = dotGO.AddComponent<Image>();
        if (dotSprite != null)
            img.sprite = dotSprite;
        else
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        img.color = dotColor;
        img.raycastTarget = false;
    }

    void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (dotGO != null)
            Destroy(dotGO);
    }

    // Optional API to show/hide the dot at runtime
    public void SetDotVisible(bool visible)
    {
        if (dotGO != null)
            dotGO.SetActive(visible);
    }
}
