using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Items.Common;

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

    public Item(ushort id, string name, string tooltip, Texture2D icon, int maxStack = 1)
    {
        Id = id;
        Name = name;
        Tooltip = tooltip;
        Icon = icon;
        MaxStack = maxStack;
    }

    public virtual ItemInstance CreateInstance(int amount = 1)
    {
        return new ItemInstance(this, amount);
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