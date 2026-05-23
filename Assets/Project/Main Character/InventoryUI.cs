using UnityEngine;
using UnityEngine.UI; // Wajib untuk mengakses komponen Image UI
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Slots Image Reference")]
    // Array untuk menampung 5 komponen Image dari objek 'ItemIcon' di Slot 1 sampai 5
    [SerializeField] private Image[] slotIcons; 
    private bool hasWarnedMissingSlots = false;

    void Awake()
    {
        ResolveSlotIcons();
    }

    // Fungsi utama untuk memperbarui tampilan gambar ikon di layar
    // Fungsi ini sekarang menerima List data Sprite, bukan string.
    public void UpdateInventoryDisplayWithSprites(List<Sprite> itemSprites)
    {
        ResolveSlotIcons();

        if (slotIcons == null || slotIcons.Length == 0)
        {
            if (!hasWarnedMissingSlots)
            {
                Debug.LogWarning("InventoryUI belum punya slotIcons. Assign ItemIcon images di Inspector atau letakkan Image bernama ItemIcon/Icon sebagai child InventoryUI.");
                hasWarnedMissingSlots = true;
            }
            return;
        }

        int itemCount = itemSprites != null ? itemSprites.Count : 0;

        // Loop sebanyak jumlah kotak slot UI yang kita miliki (5 slot)
        for (int i = 0; i < slotIcons.Length; i++)
        {
            // Pastikan komponen Image ada
            if (slotIcons[i] == null) continue;

            // Jika indeks i masih dalam jangkauan jumlah item (Sprite) yang dibawa player
            if (i < itemCount)
            {
                // Pasang gambar ikon item ke komponen Image di slot
                slotIcons[i].sprite = itemSprites[i];
                
                // Aktifkan komponen Image agar gambarnya muncul di layar
                slotIcons[i].enabled = true;
            }
            else
            {
                // Jika tidak ada item di urutan slot ini, kosongkan Sprite
                slotIcons[i].sprite = null;
                
                // Matikan komponen Image agar kotaknya terlihat kosong
                slotIcons[i].enabled = false;
            }
        }
    }

    private void ResolveSlotIcons()
    {
        if (slotIcons != null && slotIcons.Length > 0)
            return;

        Image[] childImages = GetComponentsInChildren<Image>(true);
        List<Image> iconImages = new List<Image>();

        for (int i = 0; i < childImages.Length; i++)
        {
            Image image = childImages[i];
            if (image == null)
                continue;

            string objectName = image.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("itemicon") || objectName.Contains("icon"))
                iconImages.Add(image);
        }

        slotIcons = iconImages.Count > 0 ? iconImages.ToArray() : childImages;
    }
}
