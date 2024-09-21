using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Menu
{
    public List<UIElement> Elements;
    public SpriteFont Font;

    public bool Active;

    private bool _menuSwitched;

    public Menu(SpriteFont font, bool isActive = false)
    {
        Elements = new List<UIElement>();
        Font = font;
        Active = isActive;
        
        _menuSwitched = false;
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
            
            _menuSwitched = true;
            
            OnMenuInactive();
        }
    }

    /// <summary>
    /// Invokes the OnMenuInactive method on all UI elements to handle their state when the menu becomes inactive.
    /// Outputs a debug message to indicate the method has been called.
    /// </summary>
    private void OnMenuInactive()
    {
        foreach (var elem in Elements)
        {
            elem.OnMenuInactive();
        }
        Console.WriteLine("Called OnMenuInactive on all UIElements.");
    }
    
    /// <summary>
    /// Runs in Game Update method to prevent other menus from updating when transitioning
    /// to new menu
    /// </summary>
    /// <returns></returns>
    public bool CheckAndResetMenuSwitchedFlag()
    {
        bool wasSwitched = _menuSwitched;
        _menuSwitched = false;
        return wasSwitched;
    }
}