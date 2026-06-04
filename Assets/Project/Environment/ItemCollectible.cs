using UnityEngine;

public class ItemCollectible : MonoBehaviour
{
    [Header("Item Configuration")]
    public ItemData itemData; // Masukkan aset StaminaDrinkData ke sini
    
    [Header("UI References")]
    public GameObject promptTextUI; // Tarik UI Text "[E] Untuk Mengambil" ke sini

    private bool isPlayerNearby = false;
    private PlayerInventory playerInventory;

    void Start()
    {
        if (promptTextUI != null) promptTextUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            CollectItem();
        }
    }

    void CollectItem()
    {
        if (playerInventory != null && itemData != null)
        {
            bool success = playerInventory.AddItem(itemData);
            if (success)
            {
                if (promptTextUI != null) promptTextUI.SetActive(false);
                Destroy(gameObject); // Hancurkan botol 3D di tanah
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerInventory = other.GetComponent<PlayerInventory>();

            if (promptTextUI != null) promptTextUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerInventory = null;

            if (promptTextUI != null) promptTextUI.SetActive(false);
        }
    }
}