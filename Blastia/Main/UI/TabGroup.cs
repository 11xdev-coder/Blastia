using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

/// <summary>
/// New Tab for <c>TabGroup</c>
/// </summary>
/// <param name="title">Name of the tab</param>
/// <param name="texture">Texture of the tab</param>
/// <param name="menuFactory">Function that returns <c>Menu</c> that will show when this tab is opened</param>
/// <param name="scale"></param>
/// <param name="onClick"></param>
public struct Tab(string title, Texture2D texture, Func<Menu?> menuFactory, Vector2 scale = default, Action? onClick = null)
{
    public string Title = title;
    public Vector2 Scale = scale == default ? Vector2.One : scale;
    public readonly Texture2D TabTexture = texture;
    public readonly Action? OnClick = onClick;
    
    public Menu? GetMenu() => menuFactory();
}

public class TabGroup : UIElement
{
    private const float SelectedTabUpScale = 0.4f;
    
    private readonly List<Tab> _tabsData = [];
    private readonly List<UIElement> _initializedTabs = [];
    private readonly float _tabSpacing;
    
    // for now, max menus per button is 1
    // cache only one menu
    private Menu? _cachedActiveMenu;
    private int _selectedTabIndex = -1;
    
    public TabGroup(Vector2 position, float tabSpacing, params Tab[] tabs) : base(position, BlastiaGame.TextureManager.Invisible())
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

        for (var i = 0; i < _tabsData.Count; i++)
        {
            var tabData = _tabsData[i];
            
            var tabButton = new ImageButton(startingPosition, tabData.TabTexture, tabData.OnClick)
            {
                Scale = tabData.Scale
            };

            var buttonIndex = i;
            tabButton.OnClick += () =>
            {
                if (_cachedActiveMenu != null)
                    _cachedActiveMenu.Active = false;
                
                var menu = tabData.GetMenu();
                if (menu == null) return;
                menu.HAlignOffset = HAlign;
                menu.VAlignOffset = VAlign;
                menu.Active = true;
                
                _cachedActiveMenu = menu;

                tabButton.Scale = tabData.Scale + new Vector2(SelectedTabUpScale);
                
                // apply offset immediately if didn't select yet
                if (buttonIndex != _selectedTabIndex) tabButton.Position.Y -= CalculateYOffset(tabData);
                
                _selectedTabIndex = buttonIndex;
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

    /// <summary>
    /// This method must be called when this <c>TabGroup</c> becomes inactive
    /// </summary>
    public void DeselectAll()
    {
        _selectedTabIndex = -1;
        UpdateTabs();
    }

    private float CalculateYOffset(Tab tabData) => tabData.TabTexture.Height * 0.5f * tabData.Scale.Y;

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

            // scale selected tabs
            tab.Scale =
                tabDataIndex == _selectedTabIndex ? tabData.Scale + new Vector2(SelectedTabUpScale) : tabData.Scale;
            
            tab.Position = 
                tabDataIndex == _selectedTabIndex ? 
                    new Vector2(startingPosition.X, startingPosition.Y - CalculateYOffset(tabData)) : 
                    startingPosition;
            tab.UpdateBounds();
            
            startingPosition.X += _tabSpacing + tabData.TabTexture.Width * tabData.Scale.X;
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