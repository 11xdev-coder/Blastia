using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class SelectionItem : UIElement
{
	private const int Width = 1200;
	private const int Height = 150;
	
	private readonly WorldState _worldState;
	
	private Text? _nameText;
	private Text? _metaText;
	private Button? _playButton;
	
    public SelectionItem(Vector2 position, WorldState worldState, SpriteFont font) : base(position, "", font)
	{
		_worldState = worldState;
		
		SetBackgroundProperties(() => new Rectangle(Bounds.Left, Bounds.Top, Width, Height), Colors.SelectionItemBg, 2, Colors.SelectionItemBorder, 0);
		
		_nameText = new Text(Vector2.Zero, _worldState.Name, font) 
		{
		    DrawColor = Colors.SelectionItemTextDim,
		    BorderColor = new(0, 0, 0, 0)
		};
		ChildElements.Add(_nameText);
		
		_metaText = new Text(Vector2.Zero, $"99h 59m 59s | {worldState.CreatedAtDisplay}", font) 
		{
		    Scale = new Vector2(0.7f, 0.7f),
		    DrawColor = Colors.SelectionItemMetaDim,
		    BorderColor = new(0, 0, 0, 0)
		};
		ChildElements.Add(_metaText);
		
		_playButton = new ImageButton(Vector2.Zero, BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("PlayButton", "UI", "WorldSelection"), new Vector2(3f, 3f)), Font, () => {}) 
		{
		    DrawColor = Colors.SelectionItemBorder,
		    OnStartHovering = () => {},
		    OnEndHovering = () => {},
		};
		_playButton.OnClick += SelectItem;
		ChildElements.Add(_playButton);
	}
	
	public override void Update() 
	{
	    base.Update();
	    
	    Background?.SetBackgroundColor(IsHovered ? Colors.SelectionItemBgSelected : Colors.SelectionItemBg);
        Background?.SetOutlineColor(IsHovered ? Colors.DimmedGold : Colors.SelectionItemBorder);
        ChangeDrawColor(_nameText, IsHovered ? Colors.SelectionItemText : Colors.SelectionItemTextDim);
        ChangeDrawColor(_metaText, IsHovered ? Colors.SelectionItemMeta : Colors.SelectionItemMetaDim);
        ChangeDrawColor(_playButton, IsHovered ? Colors.DimmedGold : Colors.SelectionItemBorder);
	}

    public override void UpdateBounds()
    {		
		// background may be null here so we must update bounds regardless
		
		UpdateBoundsBase(Width, Height);
		UpdateChildrenPositions();
   }
    
    private void UpdateChildrenPositions() 
    {
		if (_nameText != null)
        	_nameText.Position = new Vector2(Bounds.Left + 150, Bounds.Top + 10);
        	
        if (_metaText != null)
			_metaText.Position = new Vector2(Bounds.Left + 150, Bounds.Top + 50);
			
		if (_playButton != null)
			_playButton.Position = new Vector2(Bounds.Right - _playButton.Bounds.Width - 20, Bounds.Top + Height * 0.5f - _playButton.Bounds.Height * 0.5f);
    }
    
    /// <summary>
	/// Automatically checks element for null and changes its <c>DrawColor</c>. Helper method to save 1 line
	/// </summary>
    private void ChangeDrawColor(UIElement? e, Color newCol) 
    {
        if (e == null) return;
        e.DrawColor = newCol;
    }
    
	private void SelectItem()
	{
		var fullyLoadedState = Saving.Load<WorldState>(_worldState.FilePath);
		WorldManager.Instance.SelectWorld(fullyLoadedState, false);
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
		Text worldSelectText = new Text(new Vector2(-650 + s.X * 0.5f, -300 - s.Y), TopText, Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.59f
		};
		Elements.Add(worldSelectText);
		
		string text = $"{_worldStates.Count} items";
		s = Font.MeasureString(text);
		Text amountText = new Text(new Vector2(650 - s.X * 0.5f, -300 - s.Y), text, Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.59f
		};
		Elements.Add(amountText);
		
		AdvancedBackground bg = new AdvancedBackground(Vector2.Zero, 1300, 600, Colors.DarkBackground, 2, Colors.DarkBorder) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(bg);
		
		Viewport viewport = new Viewport(1300, 410);
		ScrollableArea area = new ScrollableArea(Vector2.Zero, viewport) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.5f
		};
		Elements.Add(area);
		
		foreach (var state in _worldStates) 
		{
		    SelectionItem item = new SelectionItem(Vector2.Zero, state, Font);
		    area.AddChild(item);
		}
		
		Console.WriteLine(Font.MeasureString("Create"));
		
		Console.WriteLine(Font.MeasureString("Delete"));
		
		Console.WriteLine(Font.MeasureString("Back"));
		
		Vector2 createScale = Font.MeasureString("Create");
		Vector2 deleteScale = Font.MeasureString("Delete");
		Vector2 backScale = Font.MeasureString("Back");
		float spacing = 15f;
		Button createButton = new Button(new Vector2(-(deleteScale.X + createScale.X) * 0.5f - spacing, 250), "Create", Font, () => SwitchToMenu(BlastiaGame.GetMenu<WorldCreationMenu>())) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(createButton);
		
		Button deleteButton = new Button(new Vector2(0, 250), "Delete", Font, () => {}) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(deleteButton);
		
		Button back = new Button(new Vector2((deleteScale.X + backScale.X) * 0.5f + spacing, 250), "Back", Font, () => SwitchToMenu(BlastiaGame.GetMenu<PlayersMenu>()))
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(back);
		
		WorldCreationButtonPreset(createButton);
		WorldCreationButtonPreset(deleteButton);
		WorldCreationButtonPreset(back);
	}

	public void ToggleMultiplayer(bool host)
	{
		Host = host;
		Console.WriteLine($"Worlds menu now host: {host}");
	}	
}