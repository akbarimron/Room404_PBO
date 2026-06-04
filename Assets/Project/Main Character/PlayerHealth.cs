using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private string deathSceneName = "deathScene";

    [Header("Dramatic Effects Settings")]
    [SerializeField] private float shakeDuration = 0.5f; 
    [SerializeField] private float shakeMagnitude = 0.2f; 
    private CameraShake cameraShake; 

    private CharacterController controller;
    private PlayerMovement playerMovement; 
    private bool isDead = false;
    [HideInInspector] public bool isHiding = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        cameraShake = GetComponentInChildren<CameraShake>();

        // Set awal jatah tengkorak ke angka 3 jika baru pertama kali bermain
        if (!PlayerPrefs.HasKey("PlayerLives"))
        {
            PlayerPrefs.SetInt("PlayerLives", 3);
            PlayerPrefs.Save();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        Die();
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Mati!");

        // Kurangi tengkorak sebanyak 1
        int currentSkulls = PlayerPrefs.GetInt("PlayerLives", 3);
        currentSkulls--;
        PlayerPrefs.SetInt("PlayerLives", currentSkulls);
        PlayerPrefs.Save();

        // Berikan efek guncangan kamera sebelum pindah scene
        if (cameraShake != null) cameraShake.Shake(shakeDuration, shakeMagnitude);

        // Matikan kontrol pergerakan
        if (controller != null) controller.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;

        // Buka deathScene secara Single (membersihkan total scene INSIDE KOS dari Hierarchy)
        SceneManager.LoadScene(deathSceneName, LoadSceneMode.Single);
    }
}