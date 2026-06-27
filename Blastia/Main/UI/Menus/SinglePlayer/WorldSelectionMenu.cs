using Blastia.Main.Persistence;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class SelectionItem : UIElement
{
	private readonly WorldState _worldState;
	
    public SelectionItem(Vector2 position, WorldState worldState, SpriteFont font) : base(position, "", font)
	{
		_worldState = worldState;
		
		AdvancedBackground bg = new AdvancedBackground(position, 700, 150, Colors.DarkBackground, 1, Colors.DarkBorder);
		ChildElements.Add(bg);
	}
}

public class WorldSelectionMenu : Menu
{
	public override ActivationMethod ActivationType => ActivationMethod.HideWhenInGame;
	
	private bool Host { get; set; }
	private static string TopText = "World Select";
	private readonly List<WorldState> _worldStates;

	public WorldSelectionMenu(SpriteFont font, bool isActive = false) : base(font, isActive, false) 
	{
	    _worldStates = WorldManager.Instance.LoadAllWorlds();
	    AddElements();
	}

    protected override void AddElements()
	{
		Vector2 s = Font.MeasureString(TopText);
		Text worldSelectText = new Text(new Vector2(-400 + s.X * 0.5f, -300 - s.Y), TopText, Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.59f
		};
		Elements.Add(worldSelectText);
		
		string text = $"{_worldStates.Count} items";
		s = Font.MeasureString(text);
		Text amountText = new Text(new Vector2(400 - s.X * 0.5f, -300 - s.Y), text, Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.59f
		};
		Elements.Add(amountText);
		
		AdvancedBackground bg = new AdvancedBackground(Vector2.Zero, 800, 600, Colors.DarkBackground, 2, Colors.DarkBorder) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(bg);
		
		SelectionItem test = new SelectionItem(Vector2.Zero, new WorldState(), Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.98f
		};
		Elements.Add(test);
	}

	public void ToggleMultiplayer(bool host)
	{
		Host = host;
		Console.WriteLine($"Worlds menu now host: {host}");
	}

	private void SelectItem(object worldState)
	{
		var fullyLoadedState = Saving.Load<WorldState>(((WorldState) worldState).FilePath);
		WorldManager.Instance.SelectWorld(fullyLoadedState, Host);
	}	
}