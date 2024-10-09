using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class ScrollableArea : UIElement
{
    private List<UIElement> _children;
    // position offset
    private float _scrolledOffset; 

    /// <summary>
    /// Rectangle in which UIElements are rendered.
    /// </summary>
    public Rectangle Viewport { get; private set; }

    /// <summary>
    /// Number of pixels scrolled per wheel tick
    /// </summary>
    public float ScrollSpeed { get; set; } = 10;
    
    public ScrollableArea(Vector2 position, Rectangle viewport, 
        float scrolledOffset = 0f) : 
        base(position, BlasterMasterGame.InvisibleTexture)
    {
        Viewport = viewport;
        _children = new List<UIElement>();
        _scrolledOffset = scrolledOffset;
    }

    public void AddChild(UIElement child)
    {
        _children.Add(child);
    }

    public override void Update()
    {
        base.Update();

        _scrolledOffset += BlasterMasterGame.ScrollWheelDelta * ScrollSpeed;
        
        // update every child
        foreach (var child in _children)
        {
            child.Update();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        
        // TODO: actually draw stuff
    }
}