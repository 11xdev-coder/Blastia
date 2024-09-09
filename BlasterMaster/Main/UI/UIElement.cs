using System.Collections.Specialized;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public abstract class UIElement
{
    /// <summary>
    /// Left corner of Bounds rectangle
    /// </summary>
    public Vector2 Position;

    public float Rotation;
    public Vector2 Scale;

    public Texture2D Texture;
    /// <summary>
    /// If true, UIElement wont draw and text, but will draw Texture
    /// </summary>
    public bool UseTexture;
    
    private float _hAlign;
    /// <summary>
    /// Horizontal Alignment
    /// </summary>
    public float HAlign
    {
        get => _hAlign;
        set => Properties.OnValueChangedProperty(ref _hAlign, value, UpdateBounds);
    }
    
    private float _vAlign;
    /// <summary>
    /// Vertical alignment
    /// </summary>
    public float VAlign
    {
        get => _vAlign;
        set => Properties.OnValueChangedProperty(ref _vAlign, value, UpdateBounds);
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
    public string Text { get; set; }
    /// <summary>
    /// Additional text variable for custom text logic
    /// </summary>
    public string InitialText { get; private set; }
    /// <summary>
    /// Draw color applied to Texture and Text
    /// </summary>
    public Color DrawColor { get; set; } = Color.White;
    
    private bool _prevIsHovered;
    
    // image
    protected UIElement(Vector2 position, Texture2D texture, Vector2 scale = default)
    {
        Position = position;
        Texture = texture;
        UseTexture = true;
        // if scale is not set -> Vector one; otherwise -> scale
        Scale = scale == default ? Vector2.One : scale;
        
        UpdateBounds();
    }
    
    // text
    protected UIElement(Vector2 position, string text, SpriteFont font)
    {
        Position = position;
        Font = font;
        Text = text;
        InitialText = text;
        
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
    
    public virtual void UpdateBounds()
    {
        Vector2 textSize = Font.MeasureString(Text);
        UpdateBoundsBase(textSize.X, textSize.Y);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (!UseTexture)
        {
            DrawText(spriteBatch);
        }
        else
        {
            DrawTexture(spriteBatch);
        }
    }

    protected void UpdateBoundsBase(float width, float height)
    {
        int positionX = (int)Position.X;
        int positionY = (int)Position.Y;

        // horizontal alignment if has any
        if (HAlign > 0)
        {
            positionX += (int)((BlasterMasterGame.ScreenWidth * HAlign) - (width * HAlign));
        }

        // vertical alignment if has any
        if (VAlign > 0)
        {
            positionY += (int)((BlasterMasterGame.ScreenHeight * VAlign) - (height * VAlign));
        }
        
        Bounds = new Rectangle(
            positionX, 
            positionY,
            (int)width + 2, 
            (int)height + 2);
    }

    protected void DrawText(SpriteBatch spriteBatch)
    {
        Vector2 textSize = Font.MeasureString(Text);
        Vector2 origin = textSize / 2f;
        Vector2 position = new Vector2(Bounds.Center.X, Bounds.Center.Y);
    
        spriteBatch.DrawString(Font, Text, position, DrawColor, Rotation, 
            origin, Vector2.One, SpriteEffects.None, 0f);
    }

    protected void DrawTexture(SpriteBatch spriteBatch)
    {
        Vector2 origin = new Vector2(Texture.Width / 2, Texture.Height / 2);
        Vector2 position = new Vector2(Bounds.Center.X, Bounds.Center.Y);
        
        spriteBatch.Draw(Texture, position, null, DrawColor, Rotation, 
            origin, Scale, SpriteEffects.None, 0f);
    }
}