using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

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
    private const string SETTINGS_KEY = "GameSettings";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
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
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(settings);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();
    }

    public GameSettings GetSettings() => settings;

    public void SetMouseSensitivity(float value)
    {
        settings.mouseSensitivity = Mathf.Clamp(value, 0.1f, 20f);
    }

    public void SetMasterVolume(float value)
    {
        settings.masterVolume = Mathf.Clamp01(value);
        AudioListener.volume = settings.masterVolume;
    }

    public void SetMusicVolume(float value) => settings.musicVolume = Mathf.Clamp01(value);
    public void SetSFXVolume(float value) => settings.sfxVolume = Mathf.Clamp01(value);
    public void SetWalkSpeed(float value) => settings.walkSpeed = Mathf.Max(1f, value);
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
}
