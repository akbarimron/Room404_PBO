using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;
    private bool isDead = false;
    [HideInInspector] public bool isHiding = false;

    [Header("Respawn Settings")]
    private Vector3 spawnPoint = new Vector3(28.5f, 0.36f, -27.78f);
    [SerializeField] private string deathSceneName = "deathScene";

    [Header("UI Reference")]
    public Image[] hearts;

    [Header("Dramatic Effects Settings")]
    [SerializeField] private float shakeDuration = 0.5f; // Durasi kamera goyang
    [SerializeField] private float shakeMagnitude = 0.2f; // Kekuatan goyangan kamera
    private CameraShake cameraShake; // Referensi ke script shake

    private CharacterController controller;
    private PlayerMovement playerMovement; // Tambahan dari perbaikan sebelumnya

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        // Otomatis mencari script CameraShake di Main Camera milik Player
        cameraShake = GetComponentInChildren<CameraShake>();

        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].enabled = (i < currentHealth);
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Mati!");

        // MEMBUAT GAME JADI SLOW MOTION (Waktu berjalan 30% dari normal)
        Time.timeScale = 0.3f;

        if (cameraShake != null)
        {
            cameraShake.Shake(shakeDuration, shakeMagnitude);
        }

        if (controller != null) controller.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;

        SceneManager.LoadScene(deathSceneName, LoadSceneMode.Additive);
        StartCoroutine(RespawnProcess());
    }

    IEnumerator RespawnProcess()
    {
        yield return new WaitForSecondsRealtime(4f);
        Time.timeScale = 1.0f;
        SceneManager.UnloadSceneAsync(deathSceneName);

        transform.position = spawnPoint;
        yield return null;

        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();

        if (controller != null) controller.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;

        Debug.Log("Player telah Respawn!");
    }
}