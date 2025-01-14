using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public struct Tab(string title, Texture2D texture, Menu? menu)
{
    public string Title = title;
    public readonly Texture2D TabTexture = texture;
    public readonly Menu? OnClickMenu = menu;
}

public class TabGroup : UIElement
{
    private readonly List<Tab> _tabsData = [];
    private readonly float _tabSpacing;
    
    public TabGroup(Vector2 position, float tabSpacing, Menu currentMenu, params Tab[] tabs) : base(position, BlastiaGame.InvisibleTexture)
    {
        _tabSpacing = tabSpacing;
        _tabsData.AddRange(tabs);
        
        Initialize(currentMenu);
    }

    private void Initialize(Menu currentMenu)
    {
        var startingPosition = Position;

        foreach (var tabData in _tabsData)
        {
            var tabButton = new ImageButton(startingPosition, tabData.TabTexture, () =>
            {
                if (tabData.OnClickMenu != null) currentMenu.SwitchToMenu(tabData.OnClickMenu);
            });
            currentMenu.Elements.Add(tabButton);
            
            startingPosition.X += _tabSpacing;
        }    
    }
}