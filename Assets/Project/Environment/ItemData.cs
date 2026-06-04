using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string itemID;
    public Sprite itemIcon; // Masukkan gambar botol/minuman bulat kamu di sini
}