using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxCapacity = 5; 

    // List ini sekarang menyimpan data Sprite (Gambar), bukan String nama item
    [Header("Current Inventory ICONS")]
    public List<Sprite> itemIcons = new List<Sprite>();

    [Header("UI Reference")]
    [SerializeField] private InventoryUI inventoryUI;

    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (inventoryUI == null)
            inventoryUI = FindInventoryUI();

        // Lakukan update UI di awal game agar semua kotak slot UI mati
        if (inventoryUI != null)
        {
            inventoryUI.UpdateInventoryDisplayWithSprites(itemIcons);
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Deteksi tombol angka 1 sampai 5
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) UseItemAtSlot(0);
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) UseItemAtSlot(1);
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) UseItemAtSlot(2);
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) UseItemAtSlot(3);
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) UseItemAtSlot(4);
    }

    // Fungsi untuk menambah item, sekarang menerima data Sprite gambarnya!
    public bool AddItem(string itemName, Sprite itemSprite)
    {
        if (itemIcons.Count >= maxCapacity)
        {
            Debug.LogWarning($"Inventory penuh!");
            return false; 
        }

        // Masukkan data Sprite ke dalam List
        itemIcons.Add(itemSprite);
        Debug.Log($"{itemName} berhasil diambil ke inventory.");

        // Perbarui tampilan layar setelah item berhasil diambil!
        if (inventoryUI == null)
            inventoryUI = FindInventoryUI();

        if (inventoryUI != null)
        {
            inventoryUI.UpdateInventoryDisplayWithSprites(itemIcons);
        }

        return true; 
    }

    public void UseItemAtSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < itemIcons.Count)
        {
            // Untuk pengecekan fungsi item, kita masih butuh nama item.
            // Kita asumsikan kita punya sistem untuk tahu sprite mana yang merupakan minuman.
            // Di sini kita cek secara manual menggunakan referensi Sprite.
            
            // CONTOH: Jika referensi sprite di slot ini adalah sprite stamina drink
            Sprite spriteToBeUsed = itemIcons[slotIndex];

            // Kita biarkan logika ini sementara kosong atau diasumsikan
            // Kita hanya implementasikan fungsi penambah stamina agar sistem berjalan.
            if (playerMovement != null)
            {
                playerMovement.IncreaseStamina(30f); 
                itemIcons.RemoveAt(slotIndex); // Hapus ikon di slot tersebut
                
                // Perbarui tampilan layar setelah item terhapus/digunakan!
                if (inventoryUI == null)
                    inventoryUI = FindInventoryUI();

                if (inventoryUI != null)
                {
                    inventoryUI.UpdateInventoryDisplayWithSprites(itemIcons);
                }
            }
        }
        else
        {
            Debug.Log($"Slot {slotIndex + 1} kosong!");
        }
    }

    private InventoryUI FindInventoryUI()
    {
        InventoryUI[] foundInventoryUis = FindObjectsByType<InventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return foundInventoryUis.Length > 0 ? foundInventoryUis[0] : null;
    }
}
