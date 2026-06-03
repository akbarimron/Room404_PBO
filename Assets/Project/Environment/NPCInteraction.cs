using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

public class NPCInteraction : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject chatBox;       
    public TextMeshProUGUI chatText; 

    [Header("NPC Dialogue Lists")]
    [TextArea(3, 5)]
    // 1. KITA UBAH INI: Menggunakan tanda [] artinya kita membuat daftar (array) kalimat
    public string[] dialogueLines; 

    private bool isPlayerNearby = false;
    private bool isChatActive = false;
    
    // 2. KITA TAMBAHKAN INI: Untuk mencatat index kalimat yang sedang aktif
    private int currentLineIndex = 0; 

    void Update()
    {
        if (isPlayerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Jika chat belum terbuka, buka kalimat pertama
            if (!isChatActive)
            {
                StartDialogue();
            }
            // Jika chat sudah terbuka, lanjut ke kalimat berikutnya saat menekan E
            else
            {
                DisplayNextLine();
            }
        }
    }

    // Fungsi untuk memulai percakapan dari awal
    void StartDialogue()
    {
        isChatActive = true;
        chatBox.SetActive(true);
        currentLineIndex = 0; // Reset ke kalimat pertama
        
        // Pastikan array tidak kosong sebelum menampilkan teks
        if (dialogueLines.Length > 0)
        {
            chatText.text = dialogueLines[currentLineIndex];
        }
    }

    // Fungsi untuk melanjutkan ke kalimat berikutnya
    void DisplayNextLine()
    {
        currentLineIndex++; // Maju ke kalimat berikutnya

        // Jika index masih di dalam jumlah kalimat yang ada, tampilkan teksnya
        if (currentLineIndex < dialogueLines.Length)
        {
            chatText.text = dialogueLines[currentLineIndex];
        }
        // Jika kalimat sudah habis, tutup kotak chat-nya
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isChatActive = false;
        chatBox.SetActive(false);
        currentLineIndex = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            EndDialogue(); // Otomatis tutup jika player pergi menjauh
        }
    }
}