using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public abstract class UIElement
{
    /// <summary>
    /// Left corner of Bounds rectangle
    /// </summary>
    public Vector2 Position;

    private float _hAlign;
    private float _vAlign;
    
    /// <summary>
    /// Horizontal Alignment
    /// </summary>
    public float HAlign
    {
        get => _hAlign;
        set
        {
            if (value != _hAlign)
            {
                _hAlign = value;
                UpdateBounds();
            }
        }
    }
    
    /// <summary>
    /// Vertical alignment
    /// </summary>
    public float VAlign
    {
        get => _vAlign;
        set
        {
            if (value != _vAlign)
            {
                _vAlign = value;
                UpdateBounds();
            }
        }
    }

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
        int cursorX = (int)BlasterMasterGame.CursorPosition.X;
        int cursorY = (int)BlasterMasterGame.CursorPosition.Y;
        bool hasClicked = BlasterMasterGame.HasClickedLeft;
        IsHovered = Bounds.Contains(cursorX, cursorY);
        
        if(IsHovered) OnHover?.Invoke(); // if hovering
        if(IsHovered && !_prevIsHovered) OnStartHovering?.Invoke(); // if started hovering
        if(!IsHovered && _prevIsHovered) OnEndHovering?.Invoke(); // end hovering
        if(IsHovered && hasClicked) OnClick?.Invoke();
        
        _prevIsHovered = IsHovered;
        
        UpdateBounds();
    }
    
    public void UpdateBounds()
    {
        Vector2 textSize = Font.MeasureString(TextToDraw);
        
        int positionX = (int)Position.X;
        int positionY = (int)Position.Y;

        // Apply horizontal alignment
        if (HAlign > 0)
        {
            positionX += (int)((BlasterMasterGame.ScreenWidth * HAlign) - (textSize.X * HAlign));
        }

        // Apply vertical alignment
        if (VAlign > 0)
        {
            positionY += (int)((BlasterMasterGame.ScreenHeight * VAlign) - (textSize.Y * VAlign));
        }
        
        Bounds = new Rectangle(
            positionX, 
            positionY,
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