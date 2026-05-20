using UnityEngine;
using TMPro; // Gunakan ini jika teks kamu menggunakan TextMeshPro
// using UnityEngine.UI; // Hapus tanda komentar '//' di kiri jika menggunakan Text biasa bawaan Unity

public class InteractionUI : MonoBehaviour
{
    // Membuat instance agar bisa dipanggil dari script mana saja tanpa ribet
    public static InteractionUI Instance { get; private set; }

    [Header("UI Component")]
    [SerializeField] private TMP_Text interactionText; // Ganti jadi 'public Text interactionText' jika pakai teks biasa

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Pastikan teks mati saat awal game
        HideText();
    }

    // Fungsi untuk menampilkan teks
    public void ShowText(string message)
    {
        if (interactionText != null)
        {
            interactionText.text = message;
            interactionText.gameObject.SetActive(true);
        }
    }

    // Fungsi untuk menyembunyikan teks
    public void HideText()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}