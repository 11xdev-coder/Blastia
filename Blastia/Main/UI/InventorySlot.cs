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
    public Vector2 IconScale { get; set; }
    
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

    public void SetItem(ItemInstance? item)
    {
        Item = item;
        UpdateVisuals();
    }

    public void ClearItem()
    {
        SetItem(null);
    }

    private void UpdateVisuals()
    {
        // if we have an item
        if (Item != null && Item.Amount > 0)
        {
            if (_itemIcon != null)
            {
                _itemIcon.Texture = Item.Icon;
                _itemIcon.Scale = IconScale;
            }

            if (_itemAmountText != null)
            {
                _itemAmountText.Text = Item.Amount.ToString();
            }
        }
        else // else clear everything
        {
            if (_itemIcon != null)
            {
                _itemIcon.Texture = BlastiaGame.InvisibleTexture;
            }

            if (_itemAmountText != null)
            {
                _itemAmountText.Text = "";
            }
        }
        
        UpdateBounds();
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();

        if (_itemIcon != null)
        {
            _itemIcon.Position = new Vector2(Bounds.Center.X, Bounds.Center.Y);
            _itemIcon.UpdateBounds();
        }

        if (_itemAmountText != null && Font != null)
        {
            Vector2 textSize = Font.MeasureString(_itemAmountText.Text) * _itemAmountText.Scale;
            _itemAmountText.Position = new Vector2(Bounds.Right - textSize.X - 5, Bounds.Bottom - textSize.Y - 5); // 5px padding
            _itemAmountText.UpdateBounds();
        }
    }

    public override void Update()
    {
        base.Update();
        
        _itemIcon?.Update();
        _itemAmountText?.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // draw slot bg
        base.Draw(spriteBatch);
        
        _itemIcon?.Draw(spriteBatch);
        _itemAmountText?.Draw(spriteBatch);
    }
}