using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Menu
{
    public List<UIElement> Elements;
    public SpriteFont Font;

    public bool Active;

    public Menu(SpriteFont font, bool isActive = true)
    {
        Elements = new List<UIElement>();
        Font = font;
        Active = isActive;
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
    
    /// <summary>
    /// Sets current menu to inactive and new menu to active
    /// </summary>
    /// <param name="menu"></param>
    public void SwitchToMenu(Menu? menu)
    {
        if (menu != null)
        {
            Active = false;
            menu.Active = true;
        }
    }
}