using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public enum AlignmentType 
{
    Left,
    Center
}

public class ScrollableArea : UIElement
{
    private List<UIElement> _children;
    public IReadOnlyList<UIElement> Children => _children; // readonly getter
    
    // position offset
    private float _scrolledOffset;
    private float _startingScrolledOffset;
    private AlignmentType _alignment;
    
    // spacing
    private float _currentSpacing;

    public int ViewportWidth;
    public int ViewportHeight;
    /// <summary>
    /// True = scroll doesn't work
    /// </summary>
    public bool ScrollLocked;

    /// <summary>
    /// Number of pixels scrolled per wheel tick
    /// </summary>
    public float ScrollSpeed { get; set; } = 0.05f;
    
    public float Spacing { get; set; }

    private RasterizerState _scissorState;
    
    public ScrollableArea(Vector2 position, Viewport viewport, AlignmentType alignmentType = AlignmentType.Center, 
        float spacing = 0f, float scrolledOffset = 0f) : 
        base(position, BlastiaGame.InvisibleTexture)
    {
        ViewportWidth = viewport.Width;
        ViewportHeight = viewport.Height;
        _alignment = alignmentType;
        Spacing = spacing;
        
        _children = new List<UIElement>();
        
        // offset
        _startingScrolledOffset = scrolledOffset;
        _scrolledOffset = CalculateStartingOffset() + _startingScrolledOffset;

        _scissorState = new RasterizerState
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None,
            FillMode = FillMode.Solid
        };
    }

    private float CalculateStartingOffset()
    {
        return BlastiaGame.ScreenHeight -
               (BlastiaGame.ScreenHeight - Bounds.Top);
    }
    
    public override void UpdateBounds()
    {
        UpdateBoundsBase(ViewportWidth, ViewportHeight);
    }

    public override void OnAlignmentChanged()
    {
        base.OnAlignmentChanged();
        
        _scrolledOffset = CalculateStartingOffset() + _startingScrolledOffset;
    }

    public void ClearChildren()
    {        
        foreach (var child in _children)
        {
            if (child is ColoredText coloredText)
            {
                coloredText.OnAnyGifLoaded -= OnChildGifLoaded;
            }
        }
        _children.Clear();
    }

    public void AddChild(UIElement child)
    {
        _children.Add(child);

        if (child is ColoredText coloredText)
            coloredText.OnAnyGifLoaded += OnChildGifLoaded;
    }
    
    /// <summary>
    /// Called when ColoredText's Gif is just loaded
    /// </summary>
    private void OnChildGifLoaded() 
    {
        ScrollToBottom();
    }

    public override void Update()
    {
        base.Update();
        
        if (!ScrollLocked) 
        {
            _currentSpacing = 0; // reset spacing
            float delta = BlastiaGame.ScrollWheelDelta * ScrollSpeed;

            var totalContentHeight = GetTotalContentHeight();
            
            // only apply scroll limits if content overflows viewport
            if (totalContentHeight > ViewportHeight)
            {
                var scrollingUp = delta > 0;
                var scrollingDown = delta < 0;
                
                // calculate scroll limits
                float topLimit = Bounds.Top; // first element's top can go to viewport top
                float bottomLimit = GetBottomLimit(totalContentHeight);
                
                // check bounds and apply scroll
                float newScrollOffset = _scrolledOffset + delta;
                
                if (scrollingDown && newScrollOffset < bottomLimit)
                {
                    _scrolledOffset = bottomLimit; // clamp to bottom limit
                }
                else if (scrollingUp && newScrollOffset > topLimit)
                {
                    _scrolledOffset = topLimit; // clamp to top limit
                }
                else
                {
                    _scrolledOffset = newScrollOffset; // normal scroll
                }
            }
        }   
        
        // update every child
        foreach (var child in _children)
        {
            // align
            switch (_alignment) 
            {
                case AlignmentType.Center:
                    child.Position.X = Bounds.Center.X - child.Bounds.Width * 0.5f;
                    break;
                case AlignmentType.Left:
                    child.Position.X = Bounds.Left;
                    break;
            }            
            
            // for each new element add spacing + height
            child.Position.Y = _scrolledOffset + _currentSpacing;
            _currentSpacing += Spacing + child.Bounds.Height;
            
            child.Update();
        }
    }
    
    private float GetTotalContentHeight() 
    {
        // calculate total content height
        float totalContentHeight = 0;
        foreach (var child in _children)
        {
            totalContentHeight += child.Bounds.Height + Spacing;
        }
        totalContentHeight -= Spacing; // remove last spacing

        return totalContentHeight;
    }
    private float GetBottomLimit(float totalContentHeight) => Bounds.Bottom - totalContentHeight; // last element's bottom matches viewport bottom

    public void ScrollToBottom() => _scrolledOffset = GetBottomLimit(GetTotalContentHeight());

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        var matrix = VideoManager.Instance.CalculateResolutionScaleMatrix();

        // transform bounds to screen coordinates
        var topLeft = Vector2.Transform(new Vector2(Bounds.X, Bounds.Y), matrix);
        var bottomRight = Vector2.Transform(new Vector2(Bounds.X + ViewportWidth, Bounds.Y + ViewportHeight), matrix);
        
        // set scissor rect
        var originalScissorRect = spriteBatch.GraphicsDevice.ScissorRectangle;
        var scissorRect = new Rectangle((int) topLeft.X, (int) topLeft.Y, (int) (bottomRight.X - topLeft.X), (int) (bottomRight.Y - topLeft.Y));

        // end current batch
        spriteBatch.End();
        BlastiaGame.BeginScissorSpriteBatch(spriteBatch, _scissorState, matrix);
        spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;
        
        // draw children
        foreach (var child in _children) 
        {
            if (child.Bounds.Bottom >= Bounds.Top && child.Bounds.Top <= Bounds.Bottom)
                child.Draw(spriteBatch);
        }

        // restore
        spriteBatch.End();
        BlastiaGame.BeginSpriteBatch(spriteBatch, matrix);
        spriteBatch.GraphicsDevice.ScissorRectangle = originalScissorRect;
    }
}

public class Viewport(int width, int height)
{
    public readonly int Width = width;
    public readonly int Height = height;
}