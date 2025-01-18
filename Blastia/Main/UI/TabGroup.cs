using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public struct Tab(string title, Texture2D texture, Func<Menu?> menuFactory, Vector2 scale = default)
{
    public string Title = title;
    public Vector2 Scale = scale == default ? Vector2.One : scale;
    public readonly Texture2D TabTexture = texture;

    public Menu? GetMenu() => menuFactory();
}

public class TabGroup : UIElement
{
    private readonly List<Tab> _tabsData = [];
    private readonly float _tabSpacing;
    
    // for now, max menus per button is 1
    // cache only one menu
    private Menu? _cachedActiveMenu;
    
    public TabGroup(Vector2 position, float tabSpacing, Menu currentMenu, params Tab[] tabs) : base(position, BlastiaGame.InvisibleTexture)
    {
        _tabSpacing = tabSpacing;
        _tabsData.AddRange(tabs);
        
        Initialize(currentMenu);
    }
    
    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        UpdateBoundsBase(Texture.Width, Texture.Height);
    }
    
    // TODO: nice menus and common methods
    private void Initialize(Menu currentMenu)
    {
        var startingPosition = new Vector2(Bounds.Left, Bounds.Top);

        foreach (var tabData in _tabsData)
        {
            var tabButton = new ImageButton(startingPosition, tabData.TabTexture, () =>
            {
                if (_cachedActiveMenu != null) _cachedActiveMenu.Active = false;
                
                var menu = tabData.GetMenu();
                if (menu == null) return;
                
                menu.Active = true;
                _cachedActiveMenu = menu;
            })
            {
                Scale = tabData.Scale
            };
            currentMenu.Elements.Add(tabButton);
            
            startingPosition.X += _tabSpacing + tabData.TabTexture.Width * tabData.Scale.X;
        }    
    }
}