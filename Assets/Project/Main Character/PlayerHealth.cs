using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;
    private bool isDead = false;

    [Header("Respawn Settings")]
    // Koordinat yang kamu minta
    private Vector3 spawnPoint = new Vector3(28.5f, 0.36f, -27.78f);

    [Header("UI Reference")]
    public Image[] hearts;
    public GameObject deathScreen; // Opsional: Panel Game Over

    private CharacterController controller;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
        UpdateHealthUI();

        // Jika ingin otomatis mengambil posisi awal saat game dimulai:
        // spawnPoint = transform.position; 
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

        // 1. Matikan kontrol gerakan segera agar player tidak bisa jalan saat mati
        if (controller != null) controller.enabled = false;

        // 2. Tampilkan UI Death
        if (deathScreen != null) deathScreen.SetActive(true);

        // 3. Jalankan proses tunggu dan respawn
        StartCoroutine(RespawnProcess());
    }

    IEnumerator RespawnProcess()
    {
        yield return new WaitForSeconds(5f);

        // Pindahkan posisi ke spawn point
        transform.position = spawnPoint;

        // Tunggu satu frame agar Unity sinkron dengan posisi baru
        yield return null;

        // Reset status kesehatan
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();

        // Sembunyikan kembali UI Death
        if (deathScreen != null) deathScreen.SetActive(false);

        // 4. Nyalakan kembali kontrol gerakan setelah 5 detik berlalu
        if (controller != null) controller.enabled = true;

        Debug.Log("Player telah Respawn dan bisa bergerak kembali!");
    }
}