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
    public Color AmountColor { get; private set; } = Color.White;
    public Vector2 IconScale { get; set; }
    public bool IsSelected { get; set; }
    
    public InventorySlot(Vector2 position, SpriteFont font, Texture2D backgroundTexture, Texture2D? highlightTexture = null, int slotIndex = -1) 
        : base(position, backgroundTexture)
    {
        Font = font;
        SlotIndex = slotIndex;
        BackgroundTexture = backgroundTexture;
        HighlightTexture = highlightTexture ?? backgroundTexture;

        _itemIcon = new Image(Vector2.Zero, BlastiaGame.InvisibleTexture);

        _itemAmountText = new Text(Vector2.Zero, "", font)
        {
            BorderColor = new Color(0, 0, 0, 0),
            DrawColor = AmountColor,
            Scale = new Vector2(0.7f, 0.7f)
        };

        OnHover += ShowTooltip;
    }

    private void ShowTooltip()
    {
        if (BlastiaGame.PlayerInventoryUiMenu == null || Item == null || !BlastiaGame.PlayerInventoryUiMenu.IsFullInventoryOpen) return;
        BlastiaGame.TooltipDisplay?.SetTooltip(Item.Name, Item.BaseItem.Type, Item.Tooltip);
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
        if (Texture == null) return;
        UpdateBoundsBase(Texture.Width, Texture.Height);

        if (_itemIcon != null && _itemIcon.Texture != null)
        {
            var halfIconWidth = _itemIcon.Texture.Width * 0.5f * IconScale.X;
            var halfIconHeight = _itemIcon.Texture.Height * 0.5f * IconScale.Y;
            
            _itemIcon.Position = new Vector2(Bounds.Center.X - halfIconWidth, Bounds.Center.Y - halfIconHeight);
            _itemIcon.Scale = IconScale;
            _itemIcon.UpdateBounds();
        }

        if (_itemAmountText != null && Font != null)
        {
            var textSize = Font.MeasureString(_itemAmountText.Text) * _itemAmountText.Scale;
            _itemAmountText.Position = new Vector2(
                Bounds.Right - textSize.X,
                Bounds.Bottom - textSize.Y
            );
            
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
        Texture = IsSelected ? HighlightTexture : BackgroundTexture;
        
        // draw slot bg
        base.Draw(spriteBatch);
        
        _itemIcon?.Draw(spriteBatch);
        _itemAmountText?.Draw(spriteBatch);
    }
}