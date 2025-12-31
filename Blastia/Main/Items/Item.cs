using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Items;

public enum ItemType
{
    Generic,
    WeaponMelee,
    WeaponRanged,
    Tool,
    Consumable,
    Placeable,
    Equipment,
    Armor,
    Material,
    Quest
}

/// <summary>
/// Base item class, has all generic properties
/// </summary>
public class Item
{
    public ushort Id { get; set; }
    public string Name { get; set; }
    public string Tooltip { get; set; }
    public Texture2D Icon { get; set; }
    public int MaxStack { get; set; }
    public ItemType Type { get; set; }
    public bool IsStackable => MaxStack > 1;

    public Item(ushort id, string name, string tooltip, Texture2D icon, int maxStack = 1, ItemType type = ItemType.Generic)
    {
        Id = id;
        Name = name;
        Tooltip = tooltip;
        Icon = icon;
        MaxStack = maxStack;
        Type = type;
    }

    public virtual ItemInstance CreateInstance(int amount = 1)
    {
        return new ItemInstance(this, amount);
    }
}

/// <summary>
/// Simple item with no special properties
/// </summary>
public class GenericItem : Item
{
    public GenericItem(ushort id, string name, string tooltip, Texture2D icon, int maxStack = 1) : base(id, name, tooltip, icon, maxStack)
    {
    }
}

/// <summary>
/// Placeable item that can be placed as blocks
/// </summary>
public class PlaceableItem : Item
{
    public ushort BlockId { get; set; }
    public string PlaceSound { get; set; }
    public ushort EmptyBucketId { get; set; }
    
    public PlaceableItem(ushort id, string name, string tooltip, Texture2D icon, int maxStack = 99, 
        ushort blockId = 0, string placeSound = "", ushort emptyBucketId = 0) : base(id, name, tooltip, icon, maxStack, ItemType.Placeable)
    {
        BlockId = blockId;
        PlaceSound = placeSound;
        EmptyBucketId = emptyBucketId;
    }
}

public class ConsumableItem : Item
{
    public int HealthRestore { get; set; }
    
    public ConsumableItem(ushort id, string name, string tooltip, Texture2D icon, int maxStack = 20, 
        int healthRestore = 0) : base(id, name, tooltip, icon, maxStack, ItemType.Consumable)
    {
        HealthRestore = healthRestore;
    }
}

/// <summary>
/// Actual instance of the item, has properties and amount. Represents item in an inventory slot
/// </summary>
public class ItemInstance
{
    public Item BaseItem { get; set; }
    private int _amount;

    public int Amount
    {
        get => _amount;
        set => _amount = Math.Clamp(value, 0, BaseItem.MaxStack);
    }

    public ItemInstance(Item item, int amount = 1)
    {
        BaseItem = item;
        Amount = amount;
    }
    
    public ushort Id => BaseItem.Id;
    public string Name => BaseItem.Name;
    public string Tooltip => BaseItem.Tooltip;
    public Texture2D Icon => BaseItem.Icon;
    public int MaxStack => BaseItem.MaxStack;
}