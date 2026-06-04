using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f; 
    [SerializeField] private float doorInteractRadius = 2.2f;
    [SerializeField] private LayerMask interactableLayer; 

    private InventoryManager inventory;
    private ItemPickup currentItem;
    private InteractiveDoor currentDoor;
    private LockerController currentLocker;

    void Start()
    {
        Debug.Log("<color=green>[PlayerInteraction]</color> Script started on " + gameObject.name);
        // Mencari InventoryManager di objek induk (First Person Player)
        inventory = GetComponentInParent<InventoryManager>();

        if (inventory == null)
        {
            Debug.LogError("InventoryManager TIDAK DITEMUKAN di Player! Pastikan script InventoryManager dipasang di objek First Person Player.");
        }
    }

    void Update()
    {
        // Skip interaction processing if the player is currently hiding
        PlayerMovement pm = GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            PlayerHealth health = pm.GetComponent<PlayerHealth>();
            if (health != null && health.isHiding)
            {
                return;
            }
        }

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Tembakkan spherecast ke depan untuk interaksi yang lebih nyaman.
        if (TryGetInteractionHit(ray, out hit))
        {
            InteractiveDoor door = hit.collider.GetComponentInParent<InteractiveDoor>();
            if (door != null)
            {
                currentItem = null;
                currentLocker = null;
                currentDoor = door;
                ShowInteractionText(currentDoor.Prompt);

                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                    currentDoor.Toggle(transform);

                return;
            }

            LockerController locker = hit.collider.GetComponentInParent<LockerController>();
            if (locker != null)
            {
                currentItem = null;
                currentDoor = null;
                currentLocker = locker;
                ShowInteractionText(currentLocker.Prompt);

                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    if (pm != null)
                    {
                        HideInteractionText();
                        currentLocker.ToggleHiding(pm.gameObject);
                    }
                }

                return;
            }

            ItemPickup item = hit.collider.GetComponentInParent<ItemPickup>();

            if (item != null)
            {
                currentDoor = null;
                currentLocker = null;

                // Jika baru melihat item ini, munculkan teks UI
                if (currentItem != item)
                {
                    currentItem = item;
                    ShowInteractionText($"Press [E] to pick up {currentItem.itemName}");
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
                                HideInteractionText();
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

                return;
            }
        }

        if (currentItem != null || currentDoor != null || currentLocker != null)
        {
            // Jika melihat ke arah lain, bersihkan target
            HideInteractionText();
            currentItem = null;
            currentDoor = null;
            currentLocker = null;
        }
    }

    private bool TryGetInteractionHit(Ray ray, out RaycastHit hit)
    {
        float castRadius = 0.25f;
        RaycastHit[] hits;

        if (interactableLayer.value != 0)
        {
            hits = Physics.SphereCastAll(ray.origin, castRadius, ray.direction, interactRange, interactableLayer, QueryTriggerInteraction.Collide);
            if (FindClosestValidHit(hits, out hit))
                return true;
        }

        hits = Physics.SphereCastAll(ray.origin, castRadius, ray.direction, interactRange, Physics.AllLayers, QueryTriggerInteraction.Collide);
        return FindClosestValidHit(hits, out hit);
    }

    private bool FindClosestValidHit(RaycastHit[] hits, out RaycastHit closestHit)
    {
        closestHit = new RaycastHit();
        float closestDistance = float.MaxValue;
        bool found = false;

        foreach (var h in hits)
        {
            // Ignore colliders on the player itself
            if (h.collider.transform.root == transform.root)
                continue;

            if (h.distance < closestDistance)
            {
                closestDistance = h.distance;
                closestHit = h;
                found = true;
            }
        }

        return found;
    }

    private void ShowInteractionText(string message)
    {
        InteractionUI ui = InteractionUI.Instance;
        if (ui != null)
        {
            ui.ShowText(message);
        }
        else
        {
            Debug.LogWarning("InteractionUI.Instance is NULL! Cannot show prompt: " + message);
        }
    }

    private void HideInteractionText()
    {
        InteractionUI ui = InteractionUI.Instance;
        if (ui != null)
            ui.HideText();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * interactRange);
    }
}
