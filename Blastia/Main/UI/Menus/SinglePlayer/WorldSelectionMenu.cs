using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldSelectionItem: AbstractSelectionItem<WorldState>
{
	private Text? _diffText;
	private Text? _sizeText;
	
    public WorldSelectionItem(Vector2 position, WorldState worldState, SpriteFont font) : base(position, worldState, font)
	{
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
	    
        ChangeDrawColor(_diffText, IsHovered || IsSelected ? Colors.SelectionItemMeta : Colors.SelectionItemMetaDim);
        ChangeDrawColor(_sizeText, IsHovered || IsSelected ? Colors.SelectionItemMeta : Colors.SelectionItemMetaDim);
	}
    
    protected override void UpdateChildrenPositions() 
    {
		base.UpdateChildrenPositions();
		
        if (_diffText != null)
			_diffText.Position = new Vector2(Bounds.Right - 300, Bounds.Top + 20);
			
        if (_sizeText != null)
			_sizeText.Position = new Vector2(Bounds.Right - 300, Bounds.Top + 50);
    }
    
	protected override void LoadState()
	{
		var fullyLoadedState = Saving.Load<WorldState>(State.FilePath);
		WorldManager.Instance.SelectWorld(fullyLoadedState, false);
	}
}

public class WorldSelectionMenu : AbstractSelectionMenu<WorldState>
{
    private bool Host { get; set; }
	protected override string TopText => "World Select";
	protected override Menu? CreationMenu => BlastiaGame.GetMenu<WorldCreationMenu>();
	protected override Menu? PreviousMenu => BlastiaGame.GetMenu<PlayerSelectionMenu>();

	public WorldSelectionMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
    }

    protected override List<WorldState> GetStates() => WorldManager.Instance.LoadAllWorlds();
    protected override WorldSelectionItem CreateSelectionItem(WorldState state) => new WorldSelectionItem(Vector2.Zero, state, Font);

    protected override bool DeleteState(string filePath) => WorldManager.Instance.DeleteWorld(filePath);

	public void ToggleMultiplayer(bool host)
	{
		Host = host;
		Console.WriteLine($"Worlds menu now host: {host}");
	}	
}