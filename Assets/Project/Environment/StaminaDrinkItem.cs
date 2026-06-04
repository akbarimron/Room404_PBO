using UnityEngine;
using UnityEngine.InputSystem; // WAJIB: Agar mengenali Input System yang baru

public class StaminaDrinkItem : MonoBehaviour
{
    [Header("Item Configuration")]
    public ItemData itemData; // Masukkan aset StaminaDrinkData ke sini
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2.5f; // Jarak maksimal player bisa mengambil botol

    private GameObject playerObject;
    private PlayerInventory playerInventory;
    private GameObject promptUI;
    private bool isInsideRange = false;

    void Start()
    {
        // 1. Cari Player secara otomatis di scene manapun via Tag
        playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerInventory = playerObject.GetComponent<PlayerInventory>();
        }

        // 2. Cari objek Teks UI bernama "InteractionPrompt" (Sekarang pasti ketemu karena objeknya aktif)
        promptUI = GameObject.Find("InteractionPrompt");
        if (promptUI != null)
        {
            // LANGSUNG MATIKAN LEWAT KODE: Agar tersembunyi saat awal game dimulai
            promptUI.SetActive(false); 
        }
    }

    void Update()
    {
        if (playerObject == null) return;

        // Hitung jarak nyata antara posisi Botol ini dengan posisi Player
        float distance = Vector3.Distance(transform.position, playerObject.transform.position);

        // Jika player masuk ke dalam jarak interaksi
        if (distance <= interactionRange)
        {
            if (!isInsideRange)
            {
                isInsideRange = true;
                if (promptUI != null) promptUI.SetActive(true); // Nyalakan teks [E]
            }

            // PERBAIKAN UTAMA: Menggunakan cara Input System baru untuk mengecek tombol E
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                CollectItem();
            }
        }
        else
        {
            // Jika player berjalan menjauh
            if (isInsideRange)
            {
                isInsideRange = false;
                if (promptUI != null) promptUI.SetActive(false); // Matikan teks [E]
            }
        }
    }

    void CollectItem()
    {
        if (playerInventory != null && itemData != null)
        {
            bool success = playerInventory.AddItem(itemData);
            if (success)
            {
                if (promptUI != null) promptUI.SetActive(false);
                Destroy(gameObject); // Hancurkan botol di lantai
            }
        }
    }
}