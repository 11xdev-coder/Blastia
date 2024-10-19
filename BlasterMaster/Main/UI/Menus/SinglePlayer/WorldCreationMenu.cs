using BlasterMaster.Main.UI.Buttons;
using BlasterMaster.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class WorldCreationMenu : Menu
{
	private Input? _worldInput;
	private Text? _worldExistsText;
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

	private void AddElements()
	{        
		Text worldNameText = new Text(Vector2.Zero, "World Name", Font)
		{
			HAlign = 0.5f,
			VAlign = 0.4f
		};
		Elements.Add(worldNameText);
		
		_worldInput = new Input(Vector2.Zero, Font, true)
		{
			HAlign = 0.5f,
			VAlign = 0.45f
		};
		Elements.Add(_worldInput);

		_worldExistsText = new Text(Vector2.Zero, "World already exists!", Font)
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			Alpha = 0f,
			DrawColor = BlasterMasterGame.ErrorColor
		};
		Elements.Add(_worldExistsText);
		
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
		
		Button createButton = new Button(Vector2.Zero, "Create", Font, CreateWorld)
		{
			HAlign = 0.5f,
			VAlign = 0.65f
		};
		Elements.Add(createButton);
		
		Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
		{
			HAlign = 0.5f,
			VAlign = 0.7f
		};
		Elements.Add(backButton);
	}
	
	private void OnClickDifficulty() 
	{
		
	}

	public override void Update()
	{
		base.Update();

		// update color
		if (_worldExistsText == null) return;
		_worldExistsText.DrawColor = BlasterMasterGame.ErrorColor;
	}

	private void CreateWorld()
	{
		int width = _sizeHandler.GetWidth();
		int height = _sizeHandler.GetHeight();
		Console.WriteLine($"World difficulty: {_difficultyHandler.CurrentItem}, Width: {width}, Height: {height}");
		
		if (_worldInput?.Text == null) return;
		string playerName = _worldInput.StringBuilder.ToString();

		if (!PlayerManager.Instance.WorldExists(playerName))
		{	
			// create world with custom difficulty if doesnt exist
			PlayerManager.Instance.NewWorld(_worldInput.StringBuilder.ToString(), 
				_difficultyHandler.CurrentItem, width, height);			
			
			Back(); // go back
		}
		else
		{
			// show text if exists
			if (_worldExistsText == null) return;

			_worldExistsText.Alpha = 1f;
			_worldExistsText.LerpAlphaToZero = true;
		}
	}

	private void Back()
	{
		SwitchToMenu(BlasterMasterGame.WorldsMenu);
	}
}