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
    
    // for now, max menus per button is 1
    // cache only one menu
    private Menu? _cachedActiveMenu;
    private ImageButton? _cachedButton;
    private Tab? _cachedTabData;
    
    public TabGroup(Vector2 position, float tabSpacing, params Tab[] tabs) : base(position, BlastiaGame.InvisibleTexture)
    {
        _tabSpacing = tabSpacing;
        _tabsData.AddRange(tabs);
        
        CreateTabs();
    }

    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        UpdateBoundsBase(Texture.Width, Texture.Height);
        
        UpdateTabs();
    }
    
    private void CreateTabs()
    {
        // remove previously initialized tabs
        if (_initializedTabs.Count > 0)
        {
            _initializedTabs.Clear();
        }
        
        var startingPosition = new Vector2(Bounds.Left, Bounds.Top);

        foreach (var tabData in _tabsData)
        {
            ImageButton tabButton = null!;
            var button = tabButton;
            
            tabButton = new ImageButton(startingPosition, tabData.TabTexture, () =>
            {
                if (_cachedActiveMenu != null) _cachedActiveMenu.Active = false;
                if (_cachedButton != null && _cachedTabData != null) // reset previous button scale
                {
                    _cachedButton.Scale = _cachedTabData.Value.Scale;
                }
                
                var menu = tabData.GetMenu();
                if (menu == null) return;
                
                menu.Active = true;
                _cachedActiveMenu = menu;
                _cachedButton = button;
                _cachedTabData = tabData;
                
                Scale = tabData.Scale + new Vector2(0.2f, 0.2f);
                
            })
            {
                Scale = tabData.Scale
            };
            _initializedTabs.Add(tabButton);
            
            startingPosition.X += _tabSpacing + tabData.TabTexture.Width * tabData.Scale.X;
        }    
    }

    public override void Update()
    {
        base.Update();

        foreach (var tab in _initializedTabs)
        {
            tab.Update();
        }
    }

    private void UpdateTabs()
    {
        if (_initializedTabs.Count <= 0) return;
        
        var tabsSet = new HashSet<UIElement>(_initializedTabs);
        var startingPosition = new Vector2(Bounds.Left, Bounds.Top);

        foreach (var tab in _initializedTabs)
        {
            if (!tabsSet.Contains(tab)) continue;
            
            var tabDataIndex = _initializedTabs.IndexOf(tab);
            if (tabDataIndex < 0) continue;
            var tabData = _tabsData[tabDataIndex];

            tab.Position = startingPosition;
            startingPosition.X += _tabSpacing + tabData.TabTexture.Width * tabData.Scale.X;
            
            tab.UpdateBounds();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        foreach (var tab in _initializedTabs)
        {
            tab.Draw(spriteBatch);
        }
    }
}