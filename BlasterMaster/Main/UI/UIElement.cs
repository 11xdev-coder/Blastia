using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public abstract class UIElement
{
    public Vector2 Position;
    // TODO: Ask how alignment works and implement
    public Vector2 Alignment;

    public Rectangle Bounds { get; set; }
    public Action? OnHover { get; set; }
    public Action? OnStartHovering { get; set; }
    public Action? OnEndHovering { get; set; }
    public Action? OnClick { get; set; }
    public bool IsHovered { get; set; }
    
    /// <summary>
    /// TextToDraw will be drawn using this Font
    /// </summary>
    public SpriteFont Font { get; set; }
    /// <summary>
    /// Text to draw in Draw method
    /// </summary>
    public string TextToDraw { get; set; }
    /// <summary>
    /// Color to draw TextToDraw
    /// </summary>
    public Color TextDrawColor { get; set; } = Color.White;
    
    private bool _prevIsHovered;

    protected UIElement()
    {
        UpdateBounds();
    }
    
    protected UIElement(Vector2 position, string text, SpriteFont font)
    {
        Position = position;
        Font = font;
        TextToDraw = text;
        
        UpdateBounds();
    }
    
    public virtual void Update()
    {
        int cursorX = (int)BlasterMasterGame.Instance.CursorPosition.X;
        int cursorY = (int)BlasterMasterGame.Instance.CursorPosition.Y;
        bool hasClicked = BlasterMasterGame.Instance.HasClickedLeft;
        IsHovered = Bounds.Contains(cursorX, cursorY);
        
        if(IsHovered) OnHover?.Invoke(); // if hovering
        if(IsHovered && !_prevIsHovered) OnStartHovering?.Invoke(); // if started hovering
        if(!IsHovered && _prevIsHovered) OnEndHovering?.Invoke(); // end hovering
        if(IsHovered && hasClicked) OnClick?.Invoke();
        
        _prevIsHovered = IsHovered;
    }
    
    public void UpdateBounds()
    {
        Vector2 textSize = Font.MeasureString(TextToDraw);
        Bounds = new Rectangle(
            (int)Position.X, 
            (int)Position.Y, 
            (int)textSize.X + 2, 
            (int)textSize.Y + 2);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        Vector2 textSize = Font.MeasureString(TextToDraw);
        Vector2 textPosition = new Vector2(
            Bounds.Center.X - textSize.X / 2,
            Bounds.Center.Y - textSize.Y / 2
        );
        spriteBatch.DrawString(Font, TextToDraw, textPosition, TextDrawColor);
    }
}