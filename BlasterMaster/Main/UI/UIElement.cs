using System.Collections.Specialized;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

// ReSharper disable once InconsistentNaming
public abstract class UIElement
{
    /// <summary>
    /// Left corner of Bounds rectangle
    /// </summary>
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;

    #region Alignment
    
    private float _hAlign;
    /// <summary>
    /// Horizontal Alignment, when changed UpdateBounds() is called
    /// </summary>
    public virtual float HAlign
    {
        get => _hAlign;
        set => Properties.OnValueChangedProperty(ref _hAlign, value, OnAlignmentChanged);
    }
    
    private float _vAlign;
    /// <summary>
    /// Vertical alignment, when changed UpdateBounds() is called
    /// </summary>
    public virtual float VAlign
    {
        get => _vAlign;
        set => Properties.OnValueChangedProperty(ref _vAlign, value, OnAlignmentChanged);
    }
    
    #endregion

    public Rectangle Bounds { get; set; }
    
    #region Hovering events
    
    public bool IsHovered { get; set; }
    private bool _prevIsHovered;
    /// <summary>
    /// Called every Update() when mouse is hovered
    /// </summary>
    public Action? OnHover { get; set; }
    /// <summary>
    /// Called once when mouse hovered
    /// </summary>
    public Action? OnStartHovering { get; set; }
    /// <summary>
    /// Called once when mouse un-hovered
    /// </summary>
    public Action? OnEndHovering { get; set; }
    
    #endregion
    
    /// <summary>
    /// Called once when LMB is released while hovered
    /// </summary>
    public Action? OnClick { get; set; }
    
    /// <summary>
    /// If clicked on this element -> focused; otherwise if clicked somewhere else -> unfocused
    /// </summary>
    public bool IsFocused { get; set; }
    
    #region Dragging
    
    public virtual bool Draggable { get; set; }
    private bool _isDragging;
    
    #endregion
    
    #region Texture
    
    public Texture2D? Texture;
    /// <summary>
    /// If true, UIElement won't Text, but will draw Texture
    /// </summary>
    public bool UseTexture;
    
    #endregion
    
    #region Text
    
    /// <summary>
    /// TextToDraw will be drawn using this Font
    /// </summary>
    public SpriteFont? Font { get; set; }
    /// <summary>
    /// Text to draw in Draw method
    /// </summary>
    public string? Text { get; set; }
    /// <summary>
    /// Additional text variable for custom text logic
    /// </summary>
    public string? InitialText { get; private set; }
    
    #endregion
    
    /// <summary>
    /// Draw color applied to Texture and Text
    /// </summary>
    public Color DrawColor { get; set; } = Color.White;
    
    /// <summary>
    /// Image constructor
    /// </summary>
    /// <param name="position">Left-top bounds corner</param>
    /// <param name="texture">Texture that will be drawn</param>
    /// <param name="scale">Texture scale (up-scaling is not recommended)</param>
    protected UIElement(Vector2 position, Texture2D texture, Vector2 scale = default)
    {
        Position = position;
        Texture = texture;
        UseTexture = true;
        // if scale is not set -> Vector one; otherwise -> scale
        Scale = scale == default ? Vector2.One : scale;
        
        Initialize();
    }

    /// <summary>
    /// Text constructor
    /// </summary>
    /// <param name="position">Left-top bounds corner</param>
    /// <param name="text">Text</param>
    /// <param name="font">Font (from menu)</param>
    protected UIElement(Vector2 position, string text, SpriteFont font)
    {
        Position = position;
        Font = font;
        Text = text;
        InitialText = text;
        
        Initialize();
    }

    private void Initialize()
    {
        UpdateBounds();
    }

    /// <summary>
    /// Invoked on menu update when the menu is inactive.
    /// It can be used to manage UI element behaviors or states while the menu is not active.
    /// </summary>
    public virtual void OnMenuInactive()
    {
        
    }
    
    /// <summary>
    /// Called when clicked on this UIElement
    /// </summary>
    public virtual void OnFocus()
    {
        IsFocused = true;
    }
    
    /// <summary>
    /// Called when clicked NOT on this UIElement (ClickedLeft + !IsHovered)
    /// </summary>
    public virtual void OnUnfocus()
    {
        IsFocused = false;
    }
    
    public virtual void Update()
    {
        int cursorX = (int)BlasterMasterGame.CursorPosition.X;
        int cursorY = (int)BlasterMasterGame.CursorPosition.Y;
        bool hasClicked = BlasterMasterGame.HasClickedLeft;
        bool isHoldingLeft = BlasterMasterGame.IsHoldingLeft;
        IsHovered = Bounds.Contains(cursorX, cursorY);
    
        if(IsHovered) OnHover?.Invoke(); // if hovering
        if(IsHovered && !_prevIsHovered) OnStartHovering?.Invoke(); // if started hovering
        if(!IsHovered && _prevIsHovered) OnEndHovering?.Invoke(); // end hovering
        if (IsHovered && hasClicked) // focus + click
        {
            OnFocus();
            OnClick?.Invoke();
        }
        if (hasClicked && !IsHovered && IsFocused) OnUnfocus(); // if clicked, not hovered and was focused -> unfocus
        
        Drag(isHoldingLeft);
    
        _prevIsHovered = IsHovered;
    
        UpdateBounds();
    }

    /// <summary>
    /// Handles the dragging logic for the UIElement based on mouse input.
    /// </summary>
    /// <param name="isHoldingLeft">Indicates if the left mouse button is being held down.</param>
    private void Drag(bool isHoldingLeft)
    {
        if (Draggable && isHoldingLeft && IsHovered && !_isDragging)
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            if (isHoldingLeft)
            {
                UpdateDragPosition(BlasterMasterGame.CursorPosition);
            }
            else
            {
                _isDragging = false;
            }
        }
    }

    /// <summary>
    /// Updates the position of the UI element being dragged based on the cursor position.
    /// </summary>
    /// <param name="cursorPosition">The current position of the cursor</param>
    protected virtual void UpdateDragPosition(Vector2 cursorPosition)
    {
        Position = GetDragPosition(cursorPosition);
    }

    /// <summary>
    /// Updates the bounding rectangle of the UI element based on its current properties such as position, text size, or texture size.
    /// </summary>
    public virtual void UpdateBounds()
    {
        if (Font == null) return;
        
        Vector2 textSize = Font.MeasureString(Text);
        UpdateBoundsBase(textSize.X, textSize.Y);
    }

    /// <summary>
    /// Updates the bounding rectangle of the UI element based on specified width and height.
    /// </summary>
    /// <param name="width">The width for the bounding rectangle.</param>
    /// <param name="height">The height for the bounding rectangle.</param>
    protected void UpdateBoundsBase(float width, float height)
    {
        float targetResX = VideoManager.Instance.TargetResolution.X;
        float targetResY = VideoManager.Instance.TargetResolution.Y;
        
        int positionX = (int)Position.X;
        int positionY = (int)Position.Y;

        if (HAlign > 0)
        {
            positionX += (int)((targetResX * HAlign) - (width * HAlign));
        }

        if (VAlign > 0)
        {
            positionY += (int)((targetResY * VAlign) - (height * VAlign));
        }

        Bounds = new Rectangle(positionX, positionY, (int)width, (int)height);
    }
    
    /// <summary>
    /// Method called whenever HAlign or VAlign is changed.
    /// </summary>
    public virtual void OnAlignmentChanged()
    {
        // update bounds by default
        UpdateBounds();
    }

    /// <summary>
    /// Subtracts alignment calculations from the X 
    /// </summary>
    /// <param name="calculatedX">X position to align</param>
    /// <returns></returns>
    protected float GetAlignedPositionX(float calculatedX)
    {
        float targetResX = VideoManager.Instance.TargetResolution.X;
        float newX = calculatedX;

        if (HAlign > 0)
        {
            newX -= (targetResX * HAlign) - (Bounds.Width * HAlign);
        }

        return newX;
    }

    /// <summary>
    /// Calculates the new position for an element being dragged based on the cursor position.
    /// </summary>
    /// <param name="cursorPosition">The current position of the cursor</param>
    /// <returns>The new position for the dragged element</returns>
    protected Vector2 GetDragPosition(Vector2 cursorPosition)
    {
        if (Font == null) return Position;
        
        Vector2 textSize = Font.MeasureString(Text);
        float targetResX = VideoManager.Instance.TargetResolution.X;
        float targetResY = VideoManager.Instance.TargetResolution.Y;

        float positionX = cursorPosition.X - textSize.X * 0.5f;
        float positionY = cursorPosition.Y - textSize.Y * 0.5f;

        if (HAlign > 0)
        {
            positionX -= (targetResX * HAlign) - (textSize.X * HAlign);
        }

        if (VAlign > 0)
        {
            positionY -= (targetResY * VAlign) - (textSize.Y * VAlign);
        }
        
        return new Vector2(positionX, positionY);
    }


    /// <summary>
    /// Calculates the drag position clamped on the X-axis while keeping the current Y position.
    /// </summary>
    /// <param name="x">The X-coordinate of the cursor position.</param>
    /// <param name="minValue">The minimum allowed X position.</param>
    /// <param name="maxValue">The maximum allowed X position.</param>
    /// <returns>A Vector2 representing the clamped X position and the current Y position.</returns>
    protected Vector2 GetDragPositionNoYClamped(float x, float minValue, float maxValue)
    {
        if (Font == null) return Position;
        
        Vector2 textSize = Font.MeasureString(Text);
        float targetResX = VideoManager.Instance.TargetResolution.X;

        float positionX = x - textSize.X * 0.5f;
        positionX = Math.Clamp(positionX, minValue, maxValue);

        if (HAlign > 0)
        {
            positionX -= (targetResX * HAlign) - (textSize.X * HAlign);
        }
        
        return new Vector2(positionX, Position.Y);
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

    private void DrawText(SpriteBatch spriteBatch)
    {
        if (Font == null) return;
        
        Vector2 textSize = Font.MeasureString(Text);
        
        Vector2 origin = textSize / 2f;
        Vector2 position = new Vector2(Bounds.Center.X, 
            Bounds.Center.Y);
        
        spriteBatch.DrawString(Font, Text, position, DrawColor, 
            Rotation, origin, Vector2.One, SpriteEffects.None, 0f);
    }

    private void DrawTexture(SpriteBatch spriteBatch)
    {
        if (Texture == null) return;
        
        Vector2 origin = new Vector2(Texture.Width * 0.5f, Texture.Height * 0.5f);
        Vector2 position = new Vector2(Bounds.Center.X, 
            Bounds.Center.Y);
        
        spriteBatch.Draw(Texture, position, null, DrawColor, Rotation, 
            origin, Scale, SpriteEffects.None, 0f);
    }
}