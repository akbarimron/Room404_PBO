using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f; 
    [SerializeField] private LayerMask interactableLayer; 

    private InventoryManager inventory;
    private ItemPickup currentItem;

    void Start()
    {
        // Mencari InventoryManager di objek induk (First Person Player)
        inventory = GetComponentInParent<InventoryManager>();

        if (inventory == null)
        {
            Debug.LogError("InventoryManager TIDAK DITEMUKAN di Player! Pastikan script InventoryManager dipasang di objek First Person Player.");
        }
    }

   void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Tembakkan laser ke depan
        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            ItemPickup item = hit.collider.GetComponent<ItemPickup>();

            if (item != null)
            {
                // Jika baru melihat item ini, munculkan teks UI
                if (currentItem != item)
                {
                    currentItem = item;
                    InteractionUI.Instance.ShowText($"Tekan [E] untuk mengambil {currentItem.itemName}");
                }

                // --- SISTEM DETEKSI INPUT YANG LEBIH AGRESIF ---
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    // Cek apakah tombol E benar-benar terdeteksi ditekan di frame ini
                    if (keyboard.eKey.wasPressedThisFrame)
                    {
                        // LOG 1: Ini HARUS muncul di console jika hardware keyboard terdeteksi
                        Debug.Log("<color=cyan>[INTERACTION]</color> Tombol E terdeteksi oleh Input System!");

                        if (inventory != null)
                        {
                            bool successfullyAdded = inventory.AddItem(currentItem.itemName, currentItem.itemIcon);
                            
                            // LOG 2: Mengecek apakah inventory mengembalikan nilai true atau false
                            Debug.Log("<color=yellow>[INTERACTION]</color> Hasil AddItem ke Inventory: " + successfullyAdded);

                            if (successfullyAdded)
                            {
                                InteractionUI.Instance.HideText();
                                Destroy(currentItem.gameObject);
                                currentItem = null;
                            }
                        }
                        else
                        {
                            // LOG 3: Jika ternyata InventoryManager-nya kosong/null
                            Debug.LogError("<color=red>[ERROR]</color> InventoryManager di Player bernilai NULL!");
                        }
                    }
                }
            }
        }
        else
        {
            // Jika melihat ke arah lain, bersihkan target
            if (currentItem != null)
            {
                InteractionUI.Instance.HideText();
                currentItem = null;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * interactRange);
    }
}