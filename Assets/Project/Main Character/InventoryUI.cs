using UnityEngine;
using UnityEngine.UI; // Wajib untuk mengakses komponen Image UI

public class InventoryUI : MonoBehaviour
{
    [Header("UI Slots Image Reference")]
    // Array untuk menampung 5 komponen Image dari objek 'ItemIcon' di Slot 1 sampai 5
    [SerializeField] private Image[] slotIcons; 

    // Fungsi utama untuk memperbarui tampilan gambar ikon di layar
    // Fungsi ini sekarang menerima List data Sprite, bukan string.
    public void UpdateInventoryDisplayWithSprites(System.Collections.Generic.List<Sprite> itemSprites)
    {
        // Loop sebanyak jumlah kotak slot UI yang kita miliki (5 slot)
        for (int i = 0; i < slotIcons.Length; i++)
        {
            // Pastikan komponen Image ada
            if (slotIcons[i] == null) continue;

            // Jika indeks i masih dalam jangkauan jumlah item (Sprite) yang dibawa player
            if (i < itemSprites.Count)
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
}