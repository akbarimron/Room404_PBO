using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    void Awake()
    {
        ResolveSettingsPanel();
    }

    void Start()
    {
        RegisterSettingsUI();

        settingsToggleButton?.onClick.AddListener(OpenSettings);
        closeButton?.onClick.AddListener(CloseSettings);
        saveButton?.onClick.AddListener(SaveSettings);
        resetButton?.onClick.AddListener(ResetSettings);

        SetupSliders();
        LoadCurrentSettings();
        settingsPanel.SetActive(false);
    }

    void OnEnable()
    {
        RegisterSettingsUI();
    }

    private void RegisterSettingsUI()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.RegisterSettingsUI(this);
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
            bobAmountSlider.maxValue = 1f;
            bobAmountSlider.onValueChanged.AddListener(OnBobAmountChanged);
        }
    }

    private void LoadCurrentSettings()
    {
        if (SettingsManager.Instance == null)
            return;

        var settings = SettingsManager.Instance.GetSettings();

        if (mouseSensitivitySlider != null) mouseSensitivitySlider.SetValueWithoutNotify(settings.mouseSensitivity);
        if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);
        if (walkSpeedSlider != null) walkSpeedSlider.SetValueWithoutNotify(settings.walkSpeed);
        if (sprintSpeedSlider != null) sprintSpeedSlider.SetValueWithoutNotify(settings.sprintSpeed);
        if (bobSpeedSlider != null) bobSpeedSlider.SetValueWithoutNotify(settings.bobSpeed);
        if (bobAmountSlider != null) bobAmountSlider.SetValueWithoutNotify(settings.bobAmount);

        UpdateMouseSensitivityText(settings.mouseSensitivity);
        UpdateMasterVolumeText(settings.masterVolume);
        UpdateMusicVolumeText(settings.musicVolume);
        UpdateSFXVolumeText(settings.sfxVolume);
        UpdateWalkSpeedText(settings.walkSpeed);
        UpdateSprintSpeedText(settings.sprintSpeed);
        UpdateBobSpeedText(settings.bobSpeed);
        UpdateBobAmountText(settings.bobAmount);
    }

    private void OnMouseSensitivityChanged(float value)
    {
        SettingsManager.Instance.SetMouseSensitivity(value);
        UpdateMouseSensitivityText(SettingsManager.Instance.GetMouseSensitivity());
    }

    private void OnMasterVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMasterVolume(value);
        UpdateMasterVolumeText(SettingsManager.Instance.GetMasterVolume());
    }

    private void OnMusicVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMusicVolume(value);
        UpdateMusicVolumeText(SettingsManager.Instance.GetMusicVolume());
    }

    private void OnSFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSFXVolume(value);
        UpdateSFXVolumeText(SettingsManager.Instance.GetSFXVolume());
    }

    private void OnWalkSpeedChanged(float value)
    {
        SettingsManager.Instance.SetWalkSpeed(value);
        float appliedWalkSpeed = SettingsManager.Instance.GetWalkSpeed();
        float appliedSprintSpeed = SettingsManager.Instance.GetSprintSpeed();

        UpdateWalkSpeedText(appliedWalkSpeed);
        UpdateSprintSpeedText(appliedSprintSpeed);

        if (sprintSpeedSlider != null)
            sprintSpeedSlider.SetValueWithoutNotify(appliedSprintSpeed);
    }

    private void OnSprintSpeedChanged(float value)
    {
        SettingsManager.Instance.SetSprintSpeed(value);
        float appliedSprintSpeed = SettingsManager.Instance.GetSprintSpeed();
        UpdateSprintSpeedText(appliedSprintSpeed);

        if (sprintSpeedSlider != null)
            sprintSpeedSlider.SetValueWithoutNotify(appliedSprintSpeed);
    }

    private void OnBobSpeedChanged(float value)
    {
        SettingsManager.Instance.SetBobSpeed(value);
        UpdateBobSpeedText(SettingsManager.Instance.GetBobSpeed());
    }

    private void OnBobAmountChanged(float value)
    {
        SettingsManager.Instance.SetBobAmount(value);
        UpdateBobAmountText(SettingsManager.Instance.GetBobAmount());
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
        ResolveSettingsPanel();
        settingsPanel.SetActive(true);
        LoadCurrentSettings();
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        ResolveSettingsPanel();
        SaveSettings();
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToggleSettings()
    {
        ResolveSettingsPanel();
        if (settingsPanel.activeSelf)
            CloseSettings();
        else
            OpenSettings();
    }

    public bool IsSettingsOpen()
    {
        ResolveSettingsPanel();
        return settingsPanel.activeSelf;
    }

    private void ResolveSettingsPanel()
    {
        if (settingsPanel == null)
            settingsPanel = gameObject;
    }

    private void UpdateMouseSensitivityText(float value)
    {
        if (mouseSensitivityText != null)
            mouseSensitivityText.text = value.ToString("F1");
    }

    private void UpdateMasterVolumeText(float value)
    {
        if (masterVolumeText != null)
            masterVolumeText.text = (value * 100).ToString("F0") + "%";
    }

    private void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
            musicVolumeText.text = (value * 100).ToString("F0") + "%";
    }

    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
            sfxVolumeText.text = (value * 100).ToString("F0") + "%";
    }

    private void UpdateWalkSpeedText(float value)
    {
        if (walkSpeedText != null)
            walkSpeedText.text = value.ToString("F1");
    }

    private void UpdateSprintSpeedText(float value)
    {
        if (sprintSpeedText != null)
            sprintSpeedText.text = value.ToString("F1");
    }

    private void UpdateBobSpeedText(float value)
    {
        if (bobSpeedText != null)
            bobSpeedText.text = value.ToString("F1");
    }

    private void UpdateBobAmountText(float value)
    {
        if (bobAmountText != null)
            bobAmountText.text = value.ToString("F2");
    }
}
