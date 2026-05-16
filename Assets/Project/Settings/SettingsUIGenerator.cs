using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIGenerator : MonoBehaviour
{
    [SerializeField] private RectTransform settingsPanel;

    [ContextMenu("Generate UI")]
    public void GenerateUI()
    {
        if (settingsPanel == null)
        {
            Debug.LogError("SettingsPanel tidak ditemukan!");
            return;
        }

        // Clear existing children
        foreach (Transform child in settingsPanel)
        {
            DestroyImmediate(child.gameObject);
        }

        // Title
        CreateText(settingsPanel, "GAME SETTINGS", 40, FontStyles.Bold);

        // Mouse Sensitivity
        CreateSliderRow(settingsPanel, "Mouse Sensitivity", 0.1f, 20f, 5f);

        // Volume Section
        CreateText(settingsPanel, "AUDIO", 30, FontStyles.Bold);
        CreateSliderRow(settingsPanel, "Master Volume", 0f, 1f, 1f);
        CreateSliderRow(settingsPanel, "Music Volume", 0f, 1f, 0.8f);
        CreateSliderRow(settingsPanel, "SFX Volume", 0f, 1f, 1f);

        // Movement Section
        CreateText(settingsPanel, "MOVEMENT", 30, FontStyles.Bold);
        CreateSliderRow(settingsPanel, "Walk Speed", 1f, 15f, 5f);
        CreateSliderRow(settingsPanel, "Sprint Speed", 5f, 25f, 10f);

        // Camera Section
        CreateText(settingsPanel, "CAMERA", 30, FontStyles.Bold);
        CreateSliderRow(settingsPanel, "Bob Speed", 1f, 20f, 8f);
        CreateSliderRow(settingsPanel, "Bob Amount", 0f, 1f, 0.3f);

        // Buttons
        CreateButtonRow(settingsPanel);

        Debug.Log("Settings UI generated successfully!");
    }

    private void CreateText(RectTransform parent, string text, int fontSize, FontStyles fontStyle)
    {
        GameObject textObj = new GameObject(text);
        textObj.transform.SetParent(parent, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAlignmentOptions.Left;
    }

    private void CreateSliderRow(RectTransform parent, string label, float minValue, float maxValue, float defaultValue)
    {
        // Container
        GameObject containerObj = new GameObject(label);
        containerObj.transform.SetParent(parent, false);
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(400, 50);

        LayoutElement layoutElement = containerObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(containerObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(150, 50);
        labelRect.anchoredPosition = new Vector2(0, 0);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 20;
        labelText.alignment = TextAlignmentOptions.Left;

        // Slider Background
        GameObject sliderBgObj = new GameObject("SliderBG");
        sliderBgObj.transform.SetParent(containerObj.transform, false);
        Image sliderBg = sliderBgObj.AddComponent<Image>();
        sliderBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        RectTransform sliderBgRect = sliderBgObj.GetComponent<RectTransform>();
        sliderBgRect.sizeDelta = new Vector2(150, 30);
        sliderBgRect.anchoredPosition = new Vector2(160, 0);

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(sliderBgObj.transform, false);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = defaultValue;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Slider Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(sliderObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0f, 1f, 0.5f, 1f);

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;

        // Value Text
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(containerObj.transform, false);
        RectTransform valueRect = valueObj.AddComponent<RectTransform>();
        valueRect.sizeDelta = new Vector2(50, 50);
        valueRect.anchoredPosition = new Vector2(330, 0);

        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = defaultValue.ToString("F1");
        valueText.fontSize = 20;
        valueText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateButtonRow(RectTransform parent)
    {
        GameObject containerObj = new GameObject("Buttons");
        containerObj.transform.SetParent(parent, false);
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(400, 60);

        LayoutElement layoutElement = containerObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 60;

        CreateButton(containerObj.transform, "Save", new Vector2(-100, 0));
        CreateButton(containerObj.transform, "Reset", new Vector2(0, 0));
        CreateButton(containerObj.transform, "Close", new Vector2(100, 0));
    }

    private void CreateButton(Transform parent, string text, Vector2 position)
    {
        GameObject btnObj = new GameObject(text + "Button");
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(80, 50);
        btnRect.anchoredPosition = position;

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        Button button = btnObj.AddComponent<Button>();

        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 20;
        buttonText.alignment = TextAlignmentOptions.Center;
    }
}
