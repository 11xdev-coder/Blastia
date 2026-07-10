using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class SelectionItem : UIElement
{
	private const int Width = 1200;
	private const int Height = 150;
	
	public readonly WorldState WorldState;
	
	private Text? _nameText;
	private Text? _metaText;
	private Text? _diffText;
	private Text? _sizeText;
	private Button? _playButton;
	
	public bool IsSelected { get; set; }
	
    public SelectionItem(Vector2 position, WorldState worldState, SpriteFont font) : base(position, "", font)
	{
		WorldState = worldState;
		
		SetBackgroundProperties(() => new Rectangle(Bounds.Left, Bounds.Top, Width, Height), Colors.SelectionItemBg, 2, Colors.SelectionItemBorder, 0);
		
		_nameText = new Text(Vector2.Zero, WorldState.Name, font) 
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
		
		_playButton = new ImageButton(Vector2.Zero, BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("PlayButton", "UI", "WorldSelection"), new Vector2(3f, 3f)), font, () => {}) 
		{
		    DrawColor = Colors.SelectionItemBorder,
		    OnStartHovering = () => {},
		    OnEndHovering = () => {},
		};
		_playButton.OnClick += SelectWorld;
		ChildElements.Add(_playButton);
		
		_diffText = new Text(Vector2.Zero, GetDifficultyDisplayName(worldState.Difficulty), font)
		{
			Scale = new Vector2(0.7f, 0.7f),
			DrawColor = Colors.SelectionItemMetaDim,
			BorderColor = new(0, 0, 0, 0)
		};
		ChildElements.Add(_diffText);
		
		_sizeText = new Text(Vector2.Zero, GetSizeDisplayName(worldState.WorldWidth, worldState.WorldHeight), font)
		{
			Scale = new Vector2(0.7f, 0.7f),
			DrawColor = Colors.SelectionItemMetaDim,
			BorderColor = new(0, 0, 0, 0)
		};
		ChildElements.Add(_sizeText);
	}
	
	private string GetDifficultyDisplayName(WorldDifficulty diff) => diff switch 
	{
	    WorldDifficulty.Easy => "Peaceful",
	    WorldDifficulty.Medium => "Unforgiving",
	    WorldDifficulty.Hard => "No mercy",
	    _ => "???"
	};
	
	private string GetSizeDisplayName(int width, int height) 
	{
		int idx = Array.FindIndex(WorldCreationMenu.SizeValues, s => s.width == width && s.height == height);
		if (idx < 0) return "error";
		
		WorldSize size = (WorldSize) idx;
		
	    return size switch 
		{
			WorldSize.Small => "Small",
			WorldSize.Medium => "Medium",
			WorldSize.Large => "Large",
			WorldSize.XL => "Enormous",
			_ => "???"
		};
	}
	
	public override void Update() 
	{
	    base.Update();
	    
	    Background?.SetBackgroundColor(IsHovered || IsSelected ? Colors.SelectionItemBgSelected : Colors.SelectionItemBg);
        Background?.SetOutlineColor(IsHovered || IsSelected ? Colors.DimmedGold : Colors.SelectionItemBorder);
        ChangeDrawColor(_nameText, IsHovered || IsSelected ? Colors.SelectionItemText : Colors.SelectionItemTextDim);
        ChangeDrawColor(_metaText, IsHovered || IsSelected ? Colors.SelectionItemMeta : Colors.SelectionItemMetaDim);
        ChangeDrawColor(_diffText, IsHovered || IsSelected ? Colors.SelectionItemMeta : Colors.SelectionItemMetaDim);
        ChangeDrawColor(_sizeText, IsHovered || IsSelected ? Colors.SelectionItemMeta : Colors.SelectionItemMetaDim);
        ChangeDrawColor(_playButton, IsHovered || IsSelected ? Colors.DimmedGold : Colors.SelectionItemBorder);
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
			
        if (_diffText != null)
			_diffText.Position = new Vector2(Bounds.Right - 300, Bounds.Top + 20);
			
        if (_sizeText != null)
			_sizeText.Position = new Vector2(Bounds.Right - 300, Bounds.Top + 50);
			
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
    
	private void SelectWorld()
	{
		var fullyLoadedState = Saving.Load<WorldState>(WorldState.FilePath);
		WorldManager.Instance.SelectWorld(fullyLoadedState, false);
	}
}

public class WorldSelectionMenu : Menu
{
	public override ActivationMethod ActivationType => ActivationMethod.HideWhenInGame;
	
	private bool Host { get; set; }
	private static string TopText = "World Select";
	private List<WorldState> _worldStates;
	private ScrollableArea? _area;
	private Text? _amountText;
	private Text? _deleteConfirmation;
	private Button? _deleteYes;
	private Button? _deleteNo;

	public WorldSelectionMenu(SpriteFont font, bool isActive = false) : base(font, isActive, false) 
	{
	    _worldStates = WorldManager.Instance.LoadAllWorlds();
	    AddElements();
	}
	
	protected override void OnMenuActive() 
	{
	    _worldStates = WorldManager.Instance.LoadAllWorlds();
	    
	    UpdateWorldStateUi();
	}
	
	private void UpdateWorldStateUi() 
	{
		_area?.ClearChildren();
		
	    foreach (var state in _worldStates) 
		{
		    SelectionItem item = new SelectionItem(Vector2.Zero, state, Font);
		    item.OnClick += () => HighlightSelectionItem(item);
		    _area?.AddChild(item);
		}
		
		if (_amountText != null)
			_amountText.Text = $"{_worldStates.Count} items";
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
		_amountText = new Text(new Vector2(650 - s.X * 0.5f, -300 - s.Y), text, Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.59f
		};
		Elements.Add(_amountText);
		
		AdvancedBackground bg = new AdvancedBackground(Vector2.Zero, 1300, 600, Colors.DarkBackground, 2, Colors.DarkBorder) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(bg);
		
		Viewport viewport = new Viewport(1300, 580);
		_area = new ScrollableArea(Vector2.Zero, viewport, spacing: 10) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(_area);
		
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
		
		Button deleteButton = new Button(new Vector2(0, 250), "Delete", Font, ShowDeleteConfirmation) 
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
		
		_deleteConfirmation = new Text(new Vector2(0, 250 + deleteScale.Y + 20), "", Font) 
		{
			HAlign = 0.5f,
			VAlign = 0.65f,
		    Alpha = 0f
		};
		Elements.Add(_deleteConfirmation);
		
		_deleteYes = new Button(new Vector2(0, 250 + deleteScale.Y + 20), "Yes", Font, () => {}) 
		{
			HAlign = 0.5f,
			VAlign = 0.65f,
		    Alpha = 0f,
		    DrawColor = Color.LimeGreen,
		    NormalColor = Color.LimeGreen,
			SelectedColor = Color.LimeGreen
		};
		Elements.Add(_deleteYes);
		
		_deleteNo = new Button(new Vector2(0, 250 + deleteScale.Y + 20), "No", Font, () => {}) 
		{
			HAlign = 0.5f,
			VAlign = 0.65f,
		    Alpha = 0f,
		    DrawColor = Color.DarkRed,
		    NormalColor = Color.DarkRed,
			SelectedColor = Color.DarkRed
		};
		Elements.Add(_deleteNo);
		
		WorldCreationButtonPreset(createButton);
		WorldCreationButtonPreset(deleteButton);
		WorldCreationButtonPreset(back);
	}
	
	private void HighlightSelectionItem(SelectionItem item) 
	{
	    if (_area == null) return;
	    
	    foreach (var ui in _area.Children) 
	    {
			if (ui is not SelectionItem selectionItem)
				continue;
				
			selectionItem.IsSelected = selectionItem == item;
	    }
	}
	
	private void ShowDeleteConfirmation() 
	{
	    if (_area == null || _deleteConfirmation == null || _deleteYes == null || _deleteNo == null) return;
	    
	    foreach (var ui in _area.Children) 
	    {
	        if (ui is not SelectionItem selectionItem)
	        	continue;
	        	
	        if (!selectionItem.IsSelected) continue;
	        
	        string text = $"Delete {selectionItem.WorldState.Name}?";
	        Vector2 size = Font.MeasureString(text);
	        _deleteConfirmation.Text = text;
	        _deleteConfirmation.Position.X = -size.X;
	        _deleteConfirmation.Alpha = 1f;
	        
	        Vector2 yesSize = Font.MeasureString("Yes");
	        Vector2 noSize = Font.MeasureString("No");
	        _deleteYes.Position.X = yesSize.X;
	        _deleteYes.Alpha = 1f;
	        _deleteNo.Position.X = yesSize.X + noSize.X + 20;
	        _deleteNo.Alpha = 1f;
	    }
	}

	public void ToggleMultiplayer(bool host)
	{
		Host = host;
		Console.WriteLine($"Worlds menu now host: {host}");
	}	
}