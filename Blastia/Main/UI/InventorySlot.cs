using Blastia.Main.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class InventorySlot : UIElement
{
    public int SlotIndex { get; private set; }
    
    public ItemInstance? Item { get; private set; }
    private Text? _itemAmountText;
    private Image? _itemIcon;
    
    // styling
    public Texture2D BackgroundTexture { get; private set; }
    public Texture2D HighlightTexture { get; private set; }
    public Color AmountColor { get; private set; }
    public Vector2 IconScale { get; private set; }
    
    public InventorySlot(Vector2 position, SpriteFont font, Texture2D backgroundTexture, Texture2D? highlightTexture = null, int slotIndex = -1) 
        : base(position, backgroundTexture)
    {
        SlotIndex = slotIndex;
        BackgroundTexture = backgroundTexture;
        HighlightTexture = highlightTexture ?? backgroundTexture;

        _itemIcon = new Image(Vector2.Zero, BlastiaGame.InvisibleTexture)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
        };

        _itemAmountText = new Text(Vector2.Zero, "", font)
        {
            DrawColor = AmountColor,
            HAlign = 0.95f,
            VAlign = 0.95f,
            Scale = new Vector2(0.7f),
            AffectedByAlignOffset = false
        };
    }
}