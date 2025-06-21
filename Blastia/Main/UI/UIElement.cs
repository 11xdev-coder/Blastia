using Blastia.Main.Sounds;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

// ReSharper disable once InconsistentNaming
public abstract class UIElement
{
    /// <summary>
    /// Left corner of Bounds rectangle
    /// </summary>
    public Vector2 Position;

    public float Rotation;
    public Vector2 Scale = Vector2.One;

    /// <summary>
    /// Scales with <c>UI Scale</c> setting
    /// </summary>
    public virtual bool Scalable { get; set; } = true;

    /// <summary>
    /// Scales with <c>CameraScale</c>
    /// </summary>
    public virtual bool ScalesWithCamera { get; set; } = false;

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

    public virtual bool AffectedByAlignOffset { get; set; } = true;

    private Vector2 _alignOffset;

    public Vector2 AlignOffset
    {
        get => AffectedByAlignOffset ? _alignOffset : Vector2.Zero;
        set => Properties.OnValueChangedProperty(ref _alignOffset, value, OnAlignmentChanged);
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
    /// Called once when RMB is released while hovered
    /// </summary>
    public Action? OnRightClick { get; set; }

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
    /// Color of the Border. Only used when rendering text (<c>UseTexture = false</c>). If border color is transparent (<c>A = 0</c>)
    /// then border is not rendered. Defaults to <c>Color.Black</c>
    /// </summary>
    public Color BorderColor { get; set; } = Color.Black;
    /// <summary>
    /// Thickness of the Border. Only used when rendering text (<c>UseTexture = false</c>). Defaults to <c>1.4f</c>
    /// </summary>
    public float BorderOffsetFactor { get; set; } = 2f;
    private readonly Vector2[] _borderOffsets =
    [
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(0, 1),
        new(-1, -1),
        new(1, -1),
        new(-1, 1),
        new(1, 1)
    ];

    public virtual float Alpha { get; set; } = 1f;
    public bool LerpAlphaToZero { get; set; }
    
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
        // if scale is not set -> Vector one; otherwse -> scale
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
        _prevIsHovered = Bounds.Contains((int)BlastiaGame.CursorPosition.X, (int)BlastiaGame.CursorPosition.Y);
    }

    /// <summary>
    /// Invoked on menu update when the menu is inactive.
    /// It can be used to manage UI element behaviors or states while the menu is not active.
    /// </summary>
    public virtual void OnMenuInactive()
    {
        // reset alpha if we should lerp it
        if (LerpAlphaToZero) Alpha = 0f;
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
        int cursorX = (int)BlastiaGame.CursorPosition.X;
        int cursorY = (int)BlastiaGame.CursorPosition.Y;
        bool hasClickedLeft = BlastiaGame.HasClickedLeft;
        bool hasClickedRight = BlastiaGame.HasClickedRight;
        bool isHoldingLeft = BlastiaGame.IsHoldingLeft;
        IsHovered = Bounds.Contains(cursorX, cursorY);

        if (IsHovered)  // if hovering
        {
            OnHover?.Invoke();
            BlastiaGame.IsHoveredOnAnyUi = true;
        }
        
        switch (IsHovered)
        {
            case true when !_prevIsHovered:
                OnStartHovering?.Invoke(); // if started hovering
                break;
            case false when _prevIsHovered:
                OnEndHovering?.Invoke(); // end hovering
                break;
        }

        if (IsHovered && hasClickedLeft) // focus + click
        {
            OnFocus();
            OnClick?.Invoke();
        }
        if (hasClickedLeft && !IsHovered && IsFocused) OnUnfocus(); // if clicked, not hovered and was focused -> unfocus

        if (IsHovered && hasClickedRight) // focus + right click
        {
            OnFocus();
            OnRightClick?.Invoke();
        }
        
        Drag(isHoldingLeft);
        ProcessAlpha();
    
        _prevIsHovered = IsHovered;
    
        UpdateBounds();
    }

    #region Dragging
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
                UpdateDragPosition(BlastiaGame.CursorPosition);
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
    #endregion
    
    private void ProcessAlpha()
    {
        // lerp alpha until 0 and reset the flag
        if (LerpAlphaToZero)
        {
            Alpha -= 0.01f;
            if (Alpha <= 0)
            {
                Alpha = 0;
                LerpAlphaToZero = false;
            }
        }
    }

    protected float GetHAlign() => HAlign + AlignOffset.X;
    protected float GetVAlign() => VAlign + AlignOffset.Y;

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
        var scaledWidth = width * Scale.X;
        var scaledHeight = height * Scale.Y;
        
        var hAlign = GetHAlign();
        var vAlign = GetVAlign();
        
        float targetResX = VideoManager.Instance.TargetResolution.X;
        float targetResY = VideoManager.Instance.TargetResolution.Y;
        
        int positionX = (int)Position.X;
        int positionY = (int)Position.Y;

        if (hAlign > 0)
        {
            positionX += (int)((targetResX * hAlign) - (scaledWidth * hAlign));
        }

        if (vAlign > 0)
        {
            positionY += (int)((targetResY * vAlign) - (scaledHeight * vAlign));
        }

        Bounds = new Rectangle(positionX, positionY, (int)scaledWidth, (int)scaledHeight);
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
        var hAlign = GetHAlign();
        
        float targetResX = VideoManager.Instance.TargetResolution.X;
        float newX = calculatedX;

        if (hAlign > 0)
        {
            newX -= (targetResX * hAlign) - (Bounds.Width * hAlign);
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
        var hAlign = GetHAlign();
        var vAlign = GetVAlign();
        
        if (Font == null) return Position;
        
        Vector2 textSize = Font.MeasureString(Text);
        float targetResX = VideoManager.Instance.TargetResolution.X;
        float targetResY = VideoManager.Instance.TargetResolution.Y;

        float positionX = cursorPosition.X - textSize.X * 0.5f;
        float positionY = cursorPosition.Y - textSize.Y * 0.5f;

        if (hAlign > 0)
        {
            positionX -= (targetResX * hAlign) - (textSize.X * hAlign);
        }

        if (vAlign > 0)
        {
            positionY -= (targetResY * vAlign) - (textSize.Y * vAlign);
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
        var hAlign = GetHAlign();
        
        if (Font == null) return Position;
        
        Vector2 textSize = Font.MeasureString(Text);
        float targetResX = VideoManager.Instance.TargetResolution.X;

        float positionX = x - textSize.X * 0.5f;
        positionX = Math.Clamp(positionX, minValue, maxValue);

        if (hAlign > 0)
        {
            positionX -= (targetResX * hAlign) - (textSize.X * hAlign);
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

        if (BorderColor.A > 0)
        {
            foreach (var offset in _borderOffsets)
            {
                spriteBatch.DrawString(Font, Text, position + offset * BorderOffsetFactor, BorderColor * Alpha, 
                    Rotation, origin, Scale, SpriteEffects.None, 0f);
            }
        }
        
        spriteBatch.DrawString(Font, Text, position, DrawColor * Alpha, 
            Rotation, origin, Scale, SpriteEffects.None, 0f);
    }

    private void DrawTexture(SpriteBatch spriteBatch)
    {
        if (Texture == null) return;
        
        Vector2 origin = new Vector2(Texture.Width * 0.5f, Texture.Height * 0.5f);
        Vector2 position = new Vector2(Bounds.Center.X, 
            Bounds.Center.Y);
        
        spriteBatch.Draw(Texture, position, null, DrawColor * Alpha, 
            Rotation, origin, Scale, SpriteEffects.None, 0f);
    }
    
    /// <summary>
    /// Plays <c>Tick</c> sound
    /// </summary>
    protected void PlayTickSound()
    {
        SoundEngine.PlaySound(SoundID.Tick);
    }
}