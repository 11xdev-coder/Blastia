﻿using Blastia.Main.GameState;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class Menu
{
    public List<UIElement> Elements;
    public SpriteFont Font;

    public bool Active;
    
    /// <summary>
    /// If <c>true</c> and player camera is initialized will use <see cref="Update(Camera)"/> to update.
    /// Otherwise, will use <see cref="Update()"/>
    /// </summary>
    public virtual bool CameraUpdate { get; set; }

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
    /// Update each element
    /// </summary>
    /// <param name="playerCamera"></param>
    public virtual void Update(Camera playerCamera)
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
        if (menu != null && menu != this && !menu.Active)
        {
            OnMenuInactive();
            
            Active = false;
            menu.Active = true;
            menu.OnMenuActive();
            
            _menuSwitched = true;
        }
    }
    
    /// <summary>
    /// Called when SwitchToMenu is called on the new menu
    /// </summary>
    public virtual void OnMenuActive()
    {
        
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