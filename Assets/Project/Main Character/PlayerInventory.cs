using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image slotIconImageUI;     // Komponen Image untuk ikon minuman
    [SerializeField] private TextMeshProUGUI stackTextUI; // Komponen TextMeshPro untuk angka (misal: "5")

    [Header("Stamina Settings")]
    [SerializeField] private float staminaBonusAmount = 50f;

    [Header("Inventory Logic")]
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int maxStackSize = 5;

    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        
        if (slots == null) slots = new List<InventorySlot>();
        slots.Clear();
        slots.Add(new InventorySlot(null, 0)); // Menambahkan slot kosong bawaan

        UpdateInventoryUI();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Tekan angka 1 untuk meminum
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            UseStaminaDrink();
        }
    }

    public bool AddItem(ItemData itemToAdd)
    {
        if (itemToAdd == null) return false;

        // 1. Jika list benar-benar kosong karena suatu hal, buatkan slot baru
        if (slots.Count == 0 || slots[0] == null)
        {
            slots.Clear();
            slots.Add(new InventorySlot(itemToAdd, 1));
            Debug.Log($"{itemToAdd.itemName} berhasil dimasukkan ke slot baru.");
            UpdateInventoryUI();
            return true;
        }

        // 2. Jika slot pertama masih kosong bawaan (Item = null), langsung isi di sini
        if (slots[0].item == null)
        {
            slots[0].item = itemToAdd;
            slots[0].count = 1;
            Debug.Log($"{itemToAdd.itemName} mengisi slot pertama yang kosong.");
            UpdateInventoryUI();
            return true;
        }

        // 3. Jika slot sudah berisi item yang sama, lakukan penumpukan (stacking) hingga maks 5
        if (slots[0].item.itemID == itemToAdd.itemID)
        {
            if (slots[0].count < maxStackSize)
            {
                slots[0].count++;
                Debug.Log($"{itemToAdd.itemName} berhasil ditumpuk. Jumlah: {slots[0].count}");
                UpdateInventoryUI();
                return true;
            }
            else
            {
                Debug.Log("Slot penuh! Maksimal tumpukan adalah 5.");
                return false;
            }
        }
        else
        {
            Debug.Log("Inventory penuh dengan item lain!");
            return false;
        }
    }

    void UseStaminaDrink()
    {
        if (slots.Count == 0 || slots[0].count <= 0)
        {
            Debug.Log("Anda tidak memiliki Minuman Stamina!");
            return;
        }

        if (playerMovement != null)
        {
            playerMovement.IncreaseStamina(staminaBonusAmount);
            Debug.Log("Minuman stamina digunakan!");
        }

        slots[0].count--;

        if (slots[0].count <= 0)
        {
            slots.Clear();
            Debug.Log("Minuman stamina habis!");
        }

        UpdateInventoryUI();
    }

    void UpdateInventoryUI()
    {
        bool hasItem = slots.Count > 0 && slots[0].count > 0;

        if (slotIconImageUI != null)
        {
            if (hasItem)
            {
                slotIconImageUI.sprite = slots[0].item.itemIcon; // Ubah gambar slot jadi gambar minuman bulat
                slotIconImageUI.enabled = true; // Nyalakan gambar
            }
            else
            {
                slotIconImageUI.sprite = null;
                slotIconImageUI.enabled = false; // Matikan gambar jika kosong
            }
        }

        if (stackTextUI != null)
        {
            if (hasItem)
            {
                stackTextUI.text = slots[0].count.ToString();
                stackTextUI.gameObject.SetActive(true); // Tampilkan teks angka jumlah stack
            }
            else
            {
                stackTextUI.gameObject.SetActive(false); // Sembunyikan angka jika kosong
            }
        }
    }
}