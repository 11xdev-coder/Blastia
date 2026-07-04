using Microsoft.Xna.Framework;

namespace Blastia.Main.Utilities;

public static class Colors 
{
    public static readonly Color TooltipBackground = new(37, 76, 167);
    public static readonly Color TooltipBorder = new(17, 47, 118);
    public static readonly Color DarkBackground = new(0, 0, 0, 190);
    public static readonly Color DarkBorder = new(255, 255, 255);
    public static readonly Color GlowingRedWarning = new(219, 0, 0);
    public static readonly Color DepletedYellowAnomaly = new(165, 142, 70);
    public static readonly Color GlowingYellowAnomaly = new(255, 194, 0);
    
    // WORLD SELECTION
    public static readonly Color SelectionItemBg = new(17, 17, 19);
    public static readonly Color SelectionItemBgSelected = new(21, 21, 16);
    public static readonly Color SelectionItemBorder = new(35, 35, 39);
    public static readonly Color SelectionItemText = new(237, 237, 237); // selected item name
    public static readonly Color SelectionItemTextDim = new(207, 207, 207); // unselected item name
    public static readonly Color SelectionItemMeta = new(122, 122, 128); // selected meta
    public static readonly Color SelectionItemMetaDim = new(106, 106, 112); // unselected meta
    public static readonly Color DimmedGold = new(217, 164, 65); // gold dim - play button, border    
}