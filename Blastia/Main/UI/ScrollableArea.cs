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
    
    // position offset
    private float _scrolledOffset;
    private float _startingScrolledOffset;
    private AlignmentType _alignment;
    
    // spacing
    private float _currentSpacing;

    public int ViewportWidth;
    public int ViewportHeight;

    /// <summary>
    /// Number of pixels scrolled per wheel tick
    /// </summary>
    public float ScrollSpeed { get; set; } = 0.05f;
    
    public float Spacing { get; set; }
    
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
        _children.Clear();
    }

    public void AddChild(UIElement child)
    {
        _children.Add(child);
    }

    public override void Update()
    {
        base.Update();
        
        _currentSpacing = 0; // reset spacing
        float delta = BlastiaGame.ScrollWheelDelta * ScrollSpeed;
        
        // cant scroll past top/bottom
        if ((delta > 0 && GetTop() >= Bounds.Top) || 
            (delta < 0 && GetBottom() <= Bounds.Bottom))
        {
            _scrolledOffset -= delta;
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

    private float GetTop()
    {
        var firstChild = _children.FirstOrDefault();

        if (firstChild != null) 
            return firstChild.Bounds.Top;

        return 0;
    }

    private float GetBottom()
    {
        var lastChild = _children.LastOrDefault();

        if (lastChild != null) 
            return lastChild.Bounds.Bottom;

        return 0;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        
        // draw if in the viewport
        foreach (var child in _children)
        {
            if (Bounds.Intersects(child.Bounds))
            {
                child.Draw(spriteBatch);
            }
        }
    }
}

public class Viewport(int width, int height)
{
    public readonly int Width = width;
    public readonly int Height = height;
}