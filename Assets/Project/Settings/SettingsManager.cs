using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                SettingsManager[] all = FindObjectsByType<SettingsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var sm in all)
                {
                    if (sm != null)
                    {
                        _instance = sm;
                        break;
                    }
                }

                if (_instance == null)
                {
                    GameObject go = new GameObject("SettingsManager");
                    _instance = go.AddComponent<SettingsManager>();
                }
            }

            if (_instance != null && _instance.gameObject != null && !_instance.gameObject.activeSelf)
            {
                _instance.gameObject.SetActive(true);
            }

            return _instance;
        }
    }

    [System.Serializable]
    public class GameSettings
    {
        public float mouseSensitivity = 5f;
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public float walkSpeed = 5f;
        public float sprintSpeed = 10f;
        public float bobSpeed = 8f;
        public float bobAmount = 0.3f;
    }

    private GameSettings settings;
    private SettingsUI settingsUI;
    private const string SETTINGS_KEY = "GameSettings";

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
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
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
        }
    }

    void Update()
    {
        if (Instance != this)
            return;

        if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            return;

        SettingsUI ui = GetSettingsUI();
        if (ui != null)
            ui.ToggleSettings();
    }

    public void LoadSettings()
    {
        string json = PlayerPrefs.GetString(SETTINGS_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            settings = new GameSettings();
        }
        else
        {
            settings = JsonUtility.FromJson<GameSettings>(json);
        }

        if (settings == null)
            settings = new GameSettings();

        ValidateSettings();
        ApplySettings();
    }

    public void SaveSettings()
    {
        ValidateSettings();
        ApplySettings();
        string json = JsonUtility.ToJson(settings);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();
    }

    public GameSettings GetSettings() => settings;

    public void RegisterSettingsUI(SettingsUI ui)
    {
        if (ui != null)
            settingsUI = ui;
    }

    public bool IsSettingsOpen()
    {
        SettingsUI ui = GetSettingsUI();
        return ui != null && ui.IsSettingsOpen();
    }

    public void ApplySettings()
    {
        if (settings == null)
            return;

        AudioListener.volume = settings.masterVolume;
    }

    private void ValidateSettings()
    {
        if (settings == null)
            settings = new GameSettings();

        settings.mouseSensitivity = Mathf.Clamp(settings.mouseSensitivity, 0.1f, 20f);
        settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
        settings.musicVolume = Mathf.Clamp01(settings.musicVolume);
        settings.sfxVolume = Mathf.Clamp01(settings.sfxVolume);
        settings.walkSpeed = Mathf.Max(1f, settings.walkSpeed);
        settings.sprintSpeed = Mathf.Max(settings.walkSpeed + 1f, settings.sprintSpeed);
        settings.bobSpeed = Mathf.Max(0.1f, settings.bobSpeed);
        settings.bobAmount = Mathf.Clamp(settings.bobAmount, 0f, 1f);
    }

    public void SetMouseSensitivity(float value)
    {
        settings.mouseSensitivity = Mathf.Clamp(value, 0.1f, 20f);
    }

    public void SetMasterVolume(float value)
    {
        settings.masterVolume = Mathf.Clamp01(value);
        ApplySettings();
    }

    public void SetMusicVolume(float value) => settings.musicVolume = Mathf.Clamp01(value);
    public void SetSFXVolume(float value) => settings.sfxVolume = Mathf.Clamp01(value);
    public void SetWalkSpeed(float value)
    {
        settings.walkSpeed = Mathf.Max(1f, value);
        settings.sprintSpeed = Mathf.Max(settings.walkSpeed + 1f, settings.sprintSpeed);
    }
    public void SetSprintSpeed(float value) => settings.sprintSpeed = Mathf.Max(settings.walkSpeed + 1f, value);
    public void SetBobSpeed(float value) => settings.bobSpeed = Mathf.Max(0.1f, value);
    public void SetBobAmount(float value) => settings.bobAmount = Mathf.Clamp(value, 0f, 1f);

    public float GetMouseSensitivity() => settings.mouseSensitivity;
    public float GetMasterVolume() => settings.masterVolume;
    public float GetMusicVolume() => settings.musicVolume;
    public float GetSFXVolume() => settings.sfxVolume;
    public float GetWalkSpeed() => settings.walkSpeed;
    public float GetSprintSpeed() => settings.sprintSpeed;
    public float GetBobSpeed() => settings.bobSpeed;
    public float GetBobAmount() => settings.bobAmount;

    private SettingsUI GetSettingsUI()
    {
        if (settingsUI != null)
            return settingsUI;

        SettingsUI[] foundSettingsUis = FindObjectsByType<SettingsUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (foundSettingsUis.Length > 0)
            settingsUI = foundSettingsUis[0];

        return settingsUI;
    }
}
