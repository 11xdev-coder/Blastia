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
		
		var sizeText = new Text(Vector2.Zero, "Size", Font) 
		{
		    HAlign = 0.15f,
		    VAlign = 0.44f
		};
		Elements.Add(sizeText);
		
		var small = new Button(Vector2.Zero, "Small", Font, () => {}, Color.Black, 0, Color.White, 5) 
		{
		    HAlign = 0.2f,
		    VAlign = 0.44f
		};
		Elements.Add(small);
		
		var medium = new Button(Vector2.Zero, "Medium", Font, () => {}, Color.Black, 0, Color.White, 5) 
		{
		    HAlign = 0.26f,
		    VAlign = 0.44f
		};
		Elements.Add(medium);
		
		var large = new Button(Vector2.Zero, "Large", Font, () => {}, Color.Black, 0, Color.White, 5) 
		{
		    HAlign = 0.33f,
		    VAlign = 0.44f
		};
		Elements.Add(large);
		
		var difficultyText = new Text(new Vector2(275, 550), "Difficulty", Font);
		Elements.Add(difficultyText);
		
		var easy = new Button(new Vector2(430, 550), "I am too young to die", Font, () => {}, Color.Black, 0, Color.White, 5);
		Elements.Add(easy);
		
		var normal = new Button(new Vector2(430, 600), "Hurt me plenty", Font, () => {}, Color.Black, 0, Color.White, 5);
		Elements.Add(normal);
		
		var hard = new Button(new Vector2(670, 600), "Nightmare", Font, () => {}, Color.Black, 0, Color.White, 5);
		Elements.Add(hard);
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