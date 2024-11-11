using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldCreationMenu : CreationMenu
{
	private HandlerArrowButton<WorldDifficulty>? _difficultyButton;
	private WorldDifficultyHandler _difficultyHandler;
	private HandlerArrowButton<WorldSize>? _sizeButton;
	private WorldSizeHandler _sizeHandler;
	
	public WorldCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		_difficultyHandler = new WorldDifficultyHandler();
		_sizeHandler = new WorldSizeHandler();
		AddElements();
	}

	protected override string GetNameLabel() => "World name";
	protected override string GetExistsText() => "World already exists!";
	protected override float CreateButtonVAlign => 0.65f;

	private void AddElements()
	{		
		_difficultyButton = new HandlerArrowButton<WorldDifficulty>(Vector2.Zero,
		"Difficulty", Font, OnClickDifficulty, 10, _difficultyHandler)
		{
			HAlign = 0.5f,
			VAlign = 0.55f
		};
		_difficultyButton.AddToElements(Elements);
		
		_sizeButton = new HandlerArrowButton<WorldSize>(Vector2.Zero,
		"World size", Font, OnClickDifficulty, 10, _sizeHandler)
		{
			HAlign = 0.5f,
			VAlign = 0.6f
		};
		_sizeButton.AddToElements(Elements);
	}
	
	private void OnClickDifficulty() 
	{
		
	}

	protected override void UpdateSpecific()
	{
		
	}

	protected override void Create()
	{
		int width = _sizeHandler.GetWidth();
		int height = _sizeHandler.GetHeight();
		Console.WriteLine($"World difficulty: {_difficultyHandler.CurrentItem}, Width: {width}, Height: {height}");
		
		if (NameInput?.Text == null) return;
		string playerName = NameInput.StringBuilder.ToString();

		if (!PlayerManager.Instance.WorldExists(playerName))
		{	
			// create world with custom difficulty if doesnt exist
			PlayerManager.Instance.NewWorld(NameInput.StringBuilder.ToString(), 
				_difficultyHandler.CurrentItem, width, height);			
			
			Back(); // go back
		}
		else
		{
			ShowExistsError();
		}
	}

	protected override void Back() => SwitchToMenu(BlastiaGame.WorldsMenu);
}