using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class DeathSceneController : MonoBehaviour
{
    [Header("Scene Destinations")]
    [SerializeField] private string insideKosSceneName = "INSIDE KOST";
    [SerializeField] private string outsideKosSceneName = "OUTSIDE KOS";
    [SerializeField] private string settingsSceneName = "Settings"; 

    [Header("UI Skull Settings")]
    [SerializeField] private Image[] skullImages; 

    private int currentLives;

    void Start()
    {
        // Munculkan kursor mouse di layar kematian
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tampilkan sisa tengkorak yang baru berkurang
        UpdateSkullUI();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Pemain melanjutkan permainan dengan menekan tombol E
        if (keyboard.eKey.wasPressedThisFrame)
        {
            HandleGameContinuation();
        }

        // ========================================================
        // SHORTCUT DEBUGGING (Bisa Anda gunakan untuk tes)
        // ========================================================
        if (keyboard.kKey.wasPressedThisFrame)
        {
            currentLives = PlayerPrefs.GetInt("PlayerLives", 3);
            currentLives--;
            if (currentLives < 0) currentLives = 0;
            
            PlayerPrefs.SetInt("PlayerLives", currentLives);
            PlayerPrefs.Save();
            
            Debug.Log("DEBUG: Tengkorak dikurangi! Sisa: " + currentLives);
            UpdateSkullUI(); 
        }

        if (keyboard.lKey.wasPressedThisFrame)
        {
            PlayerPrefs.SetInt("PlayerLives", 3);
            PlayerPrefs.Save();
            
            Debug.Log("DEBUG: Tengkorak direset kembali ke 3!");
            UpdateSkullUI(); 
        }
    }

    void UpdateSkullUI()
    {
        currentLives = PlayerPrefs.GetInt("PlayerLives", 3);

        // Nyalakan gambar tengkorak sesuai sisa nyawa di UI
        for (int i = 0; i < skullImages.Length; i++)
        {
            if (i < currentLives) skullImages[i].enabled = true; 
            else skullImages[i].enabled = false; 
        }
    }

    void HandleGameContinuation()
    {
        // Cegah objek manager ini hancur sebelum proses Coroutine pemuatan scene selesai
        DontDestroyOnLoad(this.gameObject);

        if (currentLives > 0)
        {
            // JIKA TENGKORAK MASIH ADA (1 atau 2) -> Kembali ke INSIDE KOS + Settings
            Debug.Log("Melanjutkan permainan kembali ke INSIDE KOS...");
            StartCoroutine(LoadScenesSequentially(insideKosSceneName));
        }
        else
        {
            // JIKA TENGKORAK HABIS (0) -> GAME OVER -> Pindah ke OUTSIDE KOS + Settings
            Debug.Log("Tengkorak Habis! Melempar pemain ke OUTSIDE KOS...");
            
            // Reset jatah tengkorak menjadi 3 untuk permainan baru berikutnya
            PlayerPrefs.SetInt("PlayerLives", 3);
            PlayerPrefs.Save();

            StartCoroutine(LoadScenesSequentially(outsideKosSceneName));
        }
    }

    IEnumerator LoadScenesSequentially(string mainTargetScene)
    {
        // 1. Muat Scene Utama (bisa INSIDE KOS atau OUTSIDE KOS sesuai kondisi) secara bersih
        AsyncOperation loadTarget = SceneManager.LoadSceneAsync(mainTargetScene, LoadSceneMode.Single);
        while (!loadTarget.isDone)
        {
            yield return null; 
        }

        // 2. Tumpuk dengan scene Settings agar tubuh Player dari scene tersebut ikut muncul
        AsyncOperation loadSettings = SceneManager.LoadSceneAsync(settingsSceneName, LoadSceneMode.Additive);
        while (!loadSettings.isDone)
        {
            yield return null;
        }

        // 3. Paksa Unity menjadikan Scene Utama tersebut sebagai Active Scene di Hierarchy
        Scene activeScene = SceneManager.GetSceneByName(mainTargetScene);
        if (activeScene.IsValid())
        {
            SceneManager.SetActiveScene(activeScene);
            Debug.Log("Sukses! " + mainTargetScene + " sekarang menjadi Active Scene utama.");
        }

        // Hancurkan objek sisa manager ini dari memori secara bersih
        Destroy(this.gameObject);
    }
}