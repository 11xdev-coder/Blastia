using Blastia.Main.Entities.HumanLikeEntities;

namespace Blastia.Main.Items;

public class Inventory
{
    public Player Player { get; set; }
    public List<ItemInstance?> Items { get; private set; }
    public int Capacity { get; private set; }
    public ItemInstance? CursorItem { get; private set; }

    public Inventory(int capacity, Player player)
    {
        Player = player;
        Capacity = capacity;
        Items = new(new ItemInstance?[Capacity]);
    }
    
    /// <summary>
    /// <c>int:</c> slot index; <c>ItemInstance:</c> new item (or null)
    /// </summary>
    public Action<int, ItemInstance?>? OnSlotChanged;
    /// <summary>
    ///  <c>ItemInstance:</c> new item (or null). Called whenever <c>CursorItem</c> is changed
    /// </summary>
    public Action<ItemInstance?>? OnCursorItemChanged;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <returns>Amount of items successfully added</returns>
    public int AddItem(Item? item, int amount = 1)
    {
        if (item == null || amount <= 0) return 0;

        var amountSuccessfullyAdded = 0;
        var amountRemainingToAdd = amount;
        
        // try stacking with existing items of same type
        for (int i = 0; i < Capacity; i++)
        {
            if (amountRemainingToAdd == 0) break;
            
            // same ID
            if (Items[i] != null && Items[i]!.BaseItem.Id == item.Id)
            {
                // how much can we add to this slot: 40 (MaxStack) - 12 (current amount) = 28 (can add to this slot)
                int canAddToSlot = Items[i]!.MaxStack - Items[i]!.Amount;
                if (canAddToSlot > 0)
                {
                    var toAdd = Math.Min(amountRemainingToAdd, canAddToSlot);
                    Items[i]!.Amount += toAdd;
                    amountRemainingToAdd -= toAdd;
                    amountSuccessfullyAdded += toAdd;
                    
                    OnSlotChanged?.Invoke(i, Items[i]!);
                }
            }
        }
        
        // add items to empty slot
        if (amountRemainingToAdd > 0)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (amountRemainingToAdd == 0) break;
                
                if (Items[i] == null)
                {
                    int toAdd = Math.Min(amountRemainingToAdd, item.MaxStack);
                    Items[i] = item.CreateInstance(toAdd);
                    amountRemainingToAdd -= toAdd;
                    amountSuccessfullyAdded += toAdd;
                
                    OnSlotChanged?.Invoke(i, Items[i]!);
                }
            }
        }

        return amountSuccessfullyAdded;
    }

    public ItemInstance? RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= Capacity || Items[slotIndex] == null || amount <= 0) return null;

        ItemInstance? slotItem = Items[slotIndex];
        if (slotItem == null) return null;
        
        int toRemove = Math.Min(amount, slotItem.Amount);
        ItemInstance removedPortion = slotItem.BaseItem.CreateInstance(toRemove);
        slotItem.Amount -= toRemove;

        if (slotItem.Amount <= 0)
        {
            Items[slotIndex] = null;
        }
        OnSlotChanged?.Invoke(slotIndex, Items[slotIndex]);
        
        return removedPortion;
    }

    public ItemInstance? GetItemAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Capacity) return null;
        return Items[slotIndex];
    }

    public void SetItemAt(int slotIndex, ItemInstance? item)
    {
        if (slotIndex < 0 || slotIndex >= Capacity) return;
        
        Items[slotIndex] = item;
        OnSlotChanged?.Invoke(slotIndex, item);
    }

    public void SetCursorItem(ItemInstance? item)
    {
        if (CursorItem != item)
        {
            CursorItem = item;
            OnCursorItemChanged?.Invoke(item);
        }    
    }
    
    public void SwapItems(int slotIndexA, int slotIndexB)
    {
        if (slotIndexA < 0 || slotIndexA >= Capacity || slotIndexB < 0 || slotIndexB >= Capacity) return;
        
        (Items[slotIndexA], Items[slotIndexB]) = (Items[slotIndexB], Items[slotIndexA]);
        
        OnSlotChanged?.Invoke(slotIndexA, Items[slotIndexA]);
        OnSlotChanged?.Invoke(slotIndexB, Items[slotIndexB]);
    }
}