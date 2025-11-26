using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldCreationMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
	private HandlerArrowButton<WorldDifficulty>? _difficultyButton;
	private readonly WorldDifficultyHandler _difficultyHandler = new();
	private HandlerArrowButton<WorldSize>? _sizeButton;
	private readonly WorldSizeHandler _sizeHandler = new();
	private Image? _worldPreview;
	private Image? _worldPreviewBorder;
	
	protected override void AddElements()
	{
		ColoredBackground bg = new ColoredBackground(Vector2.Zero, 1400, 600, Colors.DarkBackground, 2, Colors.DarkBorder)
		{
			HAlign = 0.5f,
			VAlign = 0.6f
		};
		Elements.Add(bg);
		
		_worldPreview = new Image(Vector2.Zero, BlastiaGame.TextureManager.Get("Preview", "UI", "WorldCreation"), 32, 32, 3, new Vector2(3.5f, 3.5f)) 
		{
		    HAlign = 0.155f,
		    VAlign = 0.32f
		};
		Elements.Add(_worldPreview);
		
		_worldPreviewBorder = new Image(Vector2.Zero, BlastiaGame.TextureManager.Get("PreviewBorder", "UI", "WorldCreation"), 36, 36, 4, new Vector2(3.5f, 3.5f)) 
		{
		    HAlign = 0.1527f,
		    VAlign = 0.317f
		};
		Elements.Add(_worldPreviewBorder);
		
		var t = new Button(Vector2.Zero, "Nigga", Font, () => {}, Color.Black, 1, Color.White) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.5f
		};
		Elements.Add(t);
		
		// _difficultyButton = new HandlerArrowButton<WorldDifficulty>(Vector2.Zero,
		// "Difficulty", Font, OnClickDifficulty, 10, _difficultyHandler)
		// {
		// 	HAlign = 0.5f,
		// 	VAlign = 0.55f
		// };
		// _difficultyButton.AddToElements(Elements);
		
		// _sizeButton = new HandlerArrowButton<WorldSize>(Vector2.Zero,
		// "World size", Font, OnClickDifficulty, 10, _sizeHandler)
		// {
		// 	HAlign = 0.5f,
		// 	VAlign = 0.6f
		// };
		// _sizeButton.AddToElements(Elements);
	}
	
	private void OnClickDifficulty() 
	{
		
	}

    public override void Update()
    {
        base.Update();
        if (_worldPreview == null) return;
    }

	// protected override void Create()
	// {
	// 	int width = _sizeHandler.GetWidth();
	// 	int height = _sizeHandler.GetHeight();
	// 	Console.WriteLine($"World difficulty: {_difficultyHandler.CurrentItem}, Width: {width}, Height: {height}");
		
	// 	if (NameInput?.Text == null) return;
	// 	string playerName = NameInput.StringBuilder.ToString();

	// 	if (!PlayerNWorldManager.Instance.WorldExists(playerName))
	// 	{	
	// 		// create world with custom difficulty if doesnt exist
	// 		PlayerNWorldManager.Instance.NewWorld(NameInput.StringBuilder.ToString(), 
	// 			_difficultyHandler.CurrentItem, width, height);			
			
	// 		Back(); // go back
	// 	}
	// 	else
	// 	{
	// 		ShowExistsError();
	// 	}
	// }
}