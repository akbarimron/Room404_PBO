using System;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int count;

    public InventorySlot(ItemData newItem, int quantity)
    {
        item = newItem;
        count = quantity;
    }
}