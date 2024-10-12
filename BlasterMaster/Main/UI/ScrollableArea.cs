using System.Reflection.Metadata.Ecma335;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class ScrollableArea : UIElement
{
    private List<UIElement> _children;
    // position offset
    private float _scrolledOffset;
    private float _currentSpacing;

    public int ViewportWidth;
    public int ViewportHeight;

    /// <summary>
    /// Number of pixels scrolled per wheel tick
    /// </summary>
    public float ScrollSpeed { get; set; } = 0.05f;
    
    public float Spacing { get; set; }
    
    public ScrollableArea(Vector2 position, Viewport viewport, float spacing = 10f,
        float scrolledOffset = 0f) : 
        base(position, BlasterMasterGame.InvisibleTexture)
    {
        ViewportWidth = viewport.Width;
        ViewportHeight = viewport.Height;
        Spacing = spacing;
        
        _children = new List<UIElement>();
        _scrolledOffset = scrolledOffset;
    }

    public override void UpdateBounds()
    {
        UpdateBoundsBase(ViewportWidth, ViewportHeight);
    }

    public void AddChild(UIElement child)
    {
        _children.Add(child);
    }

    public override void Update()
    {
        base.Update();

        _currentSpacing = 0; // reset spacing
        float delta = BlasterMasterGame.ScrollWheelDelta * ScrollSpeed;
        
        Console.WriteLine($"{GetTop()}, {Bounds.Top}");
        // cant scroll past top/bottom
        if ((delta > 0 && GetTop() >= Bounds.Top) || (delta < 0 && GetBottom() <= Bounds.Bottom))
        {
            _scrolledOffset -= delta;
        }
        
        // update every child
        foreach (var child in _children)
        {
            // for each new element add spacing
            child.Position.Y = _scrolledOffset + _currentSpacing;
            _currentSpacing += Spacing;
            
            child.Update();
        }
    }

    private float GetTop()
    {
        return _children[0].Bounds.Top;
    }

    private float GetBottom()
    {
        // last child bottom
        return _children[^1].Bounds.Bottom;
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