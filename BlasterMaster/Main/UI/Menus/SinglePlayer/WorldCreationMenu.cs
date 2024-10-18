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
	private WorldDifficultyHandler _difficultyListHandler;
	
	public WorldCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		_difficultyListHandler = new WorldDifficultyHandler();
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
		"Difficulty", Font, OnClickDifficulty, 10, _difficultyListHandler)
		{
			HAlign = 0.5f,
			VAlign = 0.55f
		};
		_difficultyButton.AddToElements(Elements);
		
		Button createButton = new Button(Vector2.Zero, "Create", Font, CreateWorld)
		{
			HAlign = 0.5f,
			VAlign = 0.6f
		};
		Elements.Add(createButton);
		
		Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
		{
			HAlign = 0.5f,
			VAlign = 0.65f
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
		Console.WriteLine(_difficultyListHandler.CurrentItem);
		
		if (_worldInput?.Text == null) return;
		string playerName = _worldInput.StringBuilder.ToString();

		if (!PlayerManager.Instance.WorldExists(playerName))
		{
			// create world with custom difficulty if doesnt exist
			PlayerManager.Instance.NewWorld(_worldInput.StringBuilder.ToString(), 
				_difficultyListHandler.CurrentItem);			
			
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