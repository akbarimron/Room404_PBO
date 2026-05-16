using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsToggleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button resetButton;

    [Header("Sensitivity Sliders")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TextMeshProUGUI mouseSensitivityText;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    [Header("Movement Sliders")]
    [SerializeField] private Slider walkSpeedSlider;
    [SerializeField] private TextMeshProUGUI walkSpeedText;
    [SerializeField] private Slider sprintSpeedSlider;
    [SerializeField] private TextMeshProUGUI sprintSpeedText;

    [Header("Camera Effect Sliders")]
    [SerializeField] private Slider bobSpeedSlider;
    [SerializeField] private TextMeshProUGUI bobSpeedText;
    [SerializeField] private Slider bobAmountSlider;
    [SerializeField] private TextMeshProUGUI bobAmountText;

    void Start()
    {
        if (settingsPanel == null)
            settingsPanel = gameObject;

        settingsToggleButton?.onClick.AddListener(OpenSettings);
        closeButton?.onClick.AddListener(CloseSettings);
        saveButton?.onClick.AddListener(SaveSettings);
        resetButton?.onClick.AddListener(ResetSettings);

        SetupSliders();
        LoadCurrentSettings();
        settingsPanel.SetActive(false);
    }

    void Update()
    {
        try
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                Debug.LogWarning("Keyboard is NULL");
                return;
            }

            var escapeKey = keyboard.escapeKey;
            if (escapeKey == null)
            {
                Debug.LogWarning("Escape key is NULL");
                return;
            }

            bool pressed = escapeKey.wasPressedThisFrame;
            Debug.Log("Escape pressed: " + pressed);

            if (pressed)
            {
                if (settingsPanel.activeSelf)
                    CloseSettings();
                else
                    OpenSettings();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Update error: " + ex.Message);
        }
    }

    private void SetupSliders()
    {
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 20f;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (walkSpeedSlider != null)
        {
            walkSpeedSlider.minValue = 1f;
            walkSpeedSlider.maxValue = 15f;
            walkSpeedSlider.onValueChanged.AddListener(OnWalkSpeedChanged);
        }

        if (sprintSpeedSlider != null)
        {
            sprintSpeedSlider.minValue = 5f;
            sprintSpeedSlider.maxValue = 25f;
            sprintSpeedSlider.onValueChanged.AddListener(OnSprintSpeedChanged);
        }

        if (bobSpeedSlider != null)
        {
            bobSpeedSlider.minValue = 1f;
            bobSpeedSlider.maxValue = 20f;
            bobSpeedSlider.onValueChanged.AddListener(OnBobSpeedChanged);
        }

        if (bobAmountSlider != null)
        {
            bobAmountSlider.minValue = 0f;
            bobAmountSlider.maxValue = 0.1f;
            bobAmountSlider.onValueChanged.AddListener(OnBobAmountChanged);
        }
    }

    private void LoadCurrentSettings()
    {
        var settings = SettingsManager.Instance.GetSettings();

        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = settings.mouseSensitivity;
        if (masterVolumeSlider != null) masterVolumeSlider.value = settings.masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = settings.musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = settings.sfxVolume;
        if (walkSpeedSlider != null) walkSpeedSlider.value = settings.walkSpeed;
        if (sprintSpeedSlider != null) sprintSpeedSlider.value = settings.sprintSpeed;
        if (bobSpeedSlider != null) bobSpeedSlider.value = settings.bobSpeed;
        if (bobAmountSlider != null) bobAmountSlider.value = settings.bobAmount;
    }

    private void OnMouseSensitivityChanged(float value)
    {
        SettingsManager.Instance.SetMouseSensitivity(value);
        if (mouseSensitivityText != null)
            mouseSensitivityText.text = value.ToString("F1");
    }

    private void OnMasterVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMasterVolume(value);
        if (masterVolumeText != null)
            masterVolumeText.text = (value * 100).ToString("F0") + "%";
    }

    private void OnMusicVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMusicVolume(value);
        if (musicVolumeText != null)
            musicVolumeText.text = (value * 100).ToString("F0") + "%";
    }

    private void OnSFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSFXVolume(value);
        if (sfxVolumeText != null)
            sfxVolumeText.text = (value * 100).ToString("F0") + "%";
    }

    private void OnWalkSpeedChanged(float value)
    {
        SettingsManager.Instance.SetWalkSpeed(value);
        if (walkSpeedText != null)
            walkSpeedText.text = value.ToString("F1");
    }

    private void OnSprintSpeedChanged(float value)
    {
        SettingsManager.Instance.SetSprintSpeed(value);
        if (sprintSpeedText != null)
            sprintSpeedText.text = value.ToString("F1");
    }

    private void OnBobSpeedChanged(float value)
    {
        SettingsManager.Instance.SetBobSpeed(value);
        if (bobSpeedText != null)
            bobSpeedText.text = value.ToString("F1");
    }

    private void OnBobAmountChanged(float value)
    {
        SettingsManager.Instance.SetBobAmount(value);
        if (bobAmountText != null)
            bobAmountText.text = value.ToString("F2");
    }

    private void SaveSettings()
    {
        SettingsManager.Instance.SaveSettings();
        Debug.Log("Settings saved!");
    }

    private void ResetSettings()
    {
        PlayerPrefs.DeleteKey("GameSettings");
        SettingsManager.Instance.LoadSettings();
        LoadCurrentSettings();
        Debug.Log("Settings reset to default!");
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        LoadCurrentSettings();
        Time.timeScale = 0f;
    }

    private void CloseSettings()
    {
        SaveSettings();
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public bool IsSettingsOpen()
    {
        return settingsPanel.activeSelf;
    }
}
