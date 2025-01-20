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
    private readonly List<UIElement> _initializedTabs = [];
    private readonly float _tabSpacing;
    private readonly Menu _currentMenu;
    
    // for now, max menus per button is 1
    // cache only one menu
    private Menu? _cachedActiveMenu;
    
    public TabGroup(Vector2 position, float tabSpacing, Menu currentMenu, params Tab[] tabs) : base(position, BlastiaGame.InvisibleTexture)
    {
        _tabSpacing = tabSpacing;
        _tabsData.AddRange(tabs);
        _currentMenu = currentMenu;
    }

    public override void OnAlignmentChanged()
    {
        base.OnAlignmentChanged();
        
        Initialize(_currentMenu);
    }

    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        UpdateBoundsBase(Texture.Width, Texture.Height);
    }
    
    private void Initialize(Menu currentMenu)
    {
        // remove previously initialized tabs
        var tabsToRemove = new HashSet<UIElement>(_initializedTabs);
        currentMenu.Elements.RemoveAll(element => tabsToRemove.Contains(element));
        
        _initializedTabs.Clear();
        
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
            _initializedTabs.Add(tabButton);
            currentMenu.Elements.Add(tabButton);
            
            startingPosition.X += _tabSpacing + tabData.TabTexture.Width * tabData.Scale.X;
        }    
    }
}