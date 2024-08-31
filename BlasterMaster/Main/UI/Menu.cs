using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Menu
{
    public List<UIElement> Elements;
    public SpriteFont Font;

    public Menu(SpriteFont font)
    {
        Elements = new List<UIElement>();
        Font = font;
        
        AddElements();
    }
    
    /// <summary>
    /// Override to add custom elements. All elements must be added to Elements list
    /// </summary>
    protected virtual void AddElements()
    {
        
    }
    
    /// <summary>
    /// Update each element
    /// </summary>
    public virtual void Update()
    {
        foreach (var elem in Elements)
        {
            elem.Update();
        }
    }
    
    /// <summary>
    /// Draw each element
    /// </summary>
    /// <param name="spriteBatch"></param>
    public virtual void Draw(SpriteBatch spriteBatch)
    {
        foreach (var elem in Elements)
        {
            elem.Draw(spriteBatch);
        }
    }
}