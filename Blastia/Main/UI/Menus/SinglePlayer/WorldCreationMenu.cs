using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldCreationMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
	private readonly WorldSizeHandler _sizeHandler = new();
	private Image? _worldPreview;
	private Image? _worldPreviewBorder;
	private Input? _name;
	
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
		
		_name = new Input(new Vector2(540, 345), Font, true, labelText: "Name") 
		{
		    CharacterLimit = 20	    
		};
		_name.SetBackgroundProperties(_name.GetBackgroundBounds, Color.Black, 1, Color.Transparent, 5);
		Elements.Add(_name);
		
		var sizeText = new Text(Vector2.Zero, "Size", Font) 
		{
		    HAlign = 0.15f,
		    VAlign = 0.44f
		};
		Elements.Add(sizeText);
		
		var small = new Button(Vector2.Zero, "Small", Font, () => {}) 
		{
		    HAlign = 0.2f,
		    VAlign = 0.44f
		};		
		Elements.Add(small);
		
		var medium = new Button(Vector2.Zero, "Medium", Font, () => {}) 
		{
		    HAlign = 0.26f,
		    VAlign = 0.44f
		};		
		Elements.Add(medium);
		
		var large = new Button(Vector2.Zero, "Large", Font, () => {}) 
		{
		    HAlign = 0.33f,
		    VAlign = 0.44f
		};		
		Elements.Add(large);
		
		WorldCreationBoolButtonPreset(small, [() => medium, () => large]);
		WorldCreationBoolButtonPreset(medium, [() => small, () => large]);
		WorldCreationBoolButtonPreset(large, [() => small, () => medium]);
		
		var difficultyText = new Text(new Vector2(275, 550), "Difficulty", Font);
		Elements.Add(difficultyText);
		
		var easy = new Button(new Vector2(430, 550), "I am too young to die", Font, () => {});
		Elements.Add(easy);
		
		var normal = new Button(new Vector2(430, 600), "Hurt me plenty", Font, () => {});
		Elements.Add(normal);
		
		var hard = new Button(new Vector2(670, 600), "Nightmare", Font, () => {});
		Elements.Add(hard);
		
		WorldCreationBoolButtonPreset(easy, [() => normal, () => hard]);
		WorldCreationBoolButtonPreset(normal, [() => easy, () => hard]);
		WorldCreationBoolButtonPreset(hard, [() => easy, () => normal]);
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