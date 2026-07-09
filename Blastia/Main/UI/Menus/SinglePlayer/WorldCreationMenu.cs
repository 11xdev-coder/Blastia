using System.Numerics;
using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.UI.Warnings;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldCreationMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
	private Image? _worldPreview;
	private Image? _worldPreviewBorder;
	private Input? _name;
	private Input? _seed;
	private ScrollableArea? _warnings;
	private Text? _tooltipText;
	private Text? _errorText;
	
	private List<Button> _sizeButtons = [];
	// sizes in order to match their buttons
	public static readonly (int width, int height)[] SizeValues = [
		(4200, 1200),
		(6400, 1800),
		(8400, 2400),
		(16800, 4800)
	];
	private List<Button> _difficultyButtons = [];
	// difficulties in order to match their buttons
	private static readonly WorldDifficulty[] DifficultyValues = [
		WorldDifficulty.Easy,
		WorldDifficulty.Medium,
		WorldDifficulty.Hard
	];
	private List<Button> _modificatorButtons = [];
	
	private void SetTooltipText(UIElement elem, string text) 
	{	
		if (_tooltipText == null) return;
		
	    elem.OnStartHovering += () => { _tooltipText.Text = text; };
	    elem.OnEndHovering += () => { _tooltipText.Text = ""; };
	}
	
	/// <summary>
	/// Get the first selected button's index from a group
	/// </summary>
	private int GetSelectedIndex(List<Button> group) => group.FindIndex(b => b.GetState());
	private (int width, int height) GetSelectedSize() 
	{
	    int idx = GetSelectedIndex(_sizeButtons);
	    return idx >= 0 ? SizeValues[idx] : SizeValues[1];
	}
	
	private WorldDifficulty GetSelectedDifficulty() 
	{
	    int idx = GetSelectedIndex(_difficultyButtons);
	    return idx >= 0 ? DifficultyValues[idx] : DifficultyValues[1];
	}
	
	protected override void AddElements()
	{
		// --------------- BACKGROUND -----------------------
		AdvancedBackground bg = new AdvancedBackground(Vector2.Zero, 1400, 600, Colors.DarkBackground, 2, Colors.DarkBorder)
		{
			HAlign = 0.5f,
			VAlign = 0.6f
		};
		Elements.Add(bg);
		
		AdvancedBackground tooltipBg = new AdvancedBackground(Vector2.Zero, 1300, 90, Colors.DarkBackground, 0) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.8f
		};
		Elements.Add(tooltipBg);
		
		_tooltipText = new Text(Vector2.Zero, "", Font)
		{
			HAlign = 0.5f,
			VAlign = 0.78f
		};
		Elements.Add(_tooltipText);
		
		// --------------- WORLD ICON -----------------------
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
		
		// --------------- NAME & SEED -----------------------
		var nameRandButton = new ImageButton(new Vector2(415, 317), BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Name", "UI", "WorldCreation"), new Vector2(2f, 2f)), Font, RandomizeWorldName);
		Elements.Add(nameRandButton);
		SetTooltipText(nameRandButton, "Randomize name");
		
		var seedRandButton = new ImageButton(new Vector2(415, 377), BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Seed", "UI", "WorldCreation"), new Vector2(2f, 2f)), Font, RandomizeSeed);
		Elements.Add(seedRandButton);
		SetTooltipText(seedRandButton, "Randomize seed");
				
		WorldCreationButtonPreset(nameRandButton);
		WorldCreationButtonPreset(seedRandButton);
		
		_name = new Input(new Vector2(565, 315), Font, true, labelText: "Name", defaultText: "") 
		{
		    CharacterLimit = 20	    
		};
		_name.SetBackgroundProperties(_name.GetBackgroundBounds, Color.Black, 1, Color.Transparent, 5);
		Elements.Add(_name);
		
		_seed = new Input(new Vector2(552, 375), Font, true, labelText: "Seed", defaultText: "") 
		{
		    CharacterLimit = 20
		};
		_seed.SetBackgroundProperties(_seed.GetBackgroundBounds, Color.Black, 1, Color.Transparent, 5);
		Elements.Add(_seed);
		
		// --------------- SIZE -----------------------
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
		SetTooltipText(small, "4200x1200");
		
		var medium = new Button(Vector2.Zero, "Medium", Font, () => {}) 
		{
		    HAlign = 0.26f,
		    VAlign = 0.44f
		};		
		Elements.Add(medium);
		SetTooltipText(medium, "6400x1800");
		
		var large = new Button(Vector2.Zero, "Large", Font, () => {}) 
		{
		    HAlign = 0.33f,
		    VAlign = 0.44f
		};
		Elements.Add(large);
		SetTooltipText(large, "8400x2400");
				
		var xl = new Button(Vector2.Zero, "XL", Font, () => {}) 
		{
		    HAlign = 0.375f,
		    VAlign = 0.44f
		};		
		Elements.Add(xl);
		SetTooltipText(xl, "16800x4800");
		
		WorldCreationBoolButtonPreset(small, [() => medium, () => large, () => xl], false);
		WorldCreationBoolButtonPreset(medium, [() => small, () => large, () => xl], false);
		WorldCreationBoolButtonPreset(large, [() => small, () => medium, () => xl], false);
		WorldCreationBoolButtonPreset(xl, [() => small, () => medium, () => large], false);
		_sizeButtons.AddRange([small, medium, large, xl]);
		
		// --------------- DIFFICULTY -----------------------
		var difficultyText = new Text(new Vector2(275, 550), "Difficulty", Font);
		Elements.Add(difficultyText);
		
		var easy = new Button(new Vector2(430, 550), "Peaceful", Font, () => {});
		Elements.Add(easy);
		SetTooltipText(easy, "The standard Blastia experience");
		
		var normal = new Button(new Vector2(430, 605), "Unforgiving", Font, () => {});
		Elements.Add(normal);
		SetTooltipText(normal, "Greater difficulty with better loot");
		
		var hard = new Button(new Vector2(670, 605), "No mercy", Font, () => {});
		Elements.Add(hard);		
		SetTooltipText(hard, "For those who'd like a challenge");
		
		WorldCreationBoolButtonPreset(easy, [() => normal, () => hard], false);
		WorldCreationBoolButtonPreset(normal, [() => easy, () => hard], false);
		WorldCreationBoolButtonPreset(hard, [() => easy, () => normal], false);
		_difficultyButtons.AddRange([easy, normal, hard]);
		
		// --------------- WARNINGS -----------------------
		var viewport = new Viewport(400, 500);
		_warnings = new ScrollableArea(new Vector2(1280, 330), viewport, AlignmentType.Left, 20);
		Elements.Add(_warnings);
		
		// --------------- MODIFICATORS -----------------------
		var lowGravity = new Button(new Vector2(430, 680), "Low gravity", Font, UpdateModificators);
		Elements.Add(lowGravity);
		SetTooltipText(lowGravity, "Lower gravity (0.7x)");
		var highGravity = new Button(new Vector2(620, 680), "High gravity", Font, UpdateModificators);
		Elements.Add(highGravity);
		SetTooltipText(highGravity, "Higher gravity (1.5x)");
		WorldCreationBoolButtonPreset(lowGravity, [() => highGravity]);
		WorldCreationBoolButtonPreset(highGravity, [() => lowGravity]);
		
		var eternalWinter = new Button(new Vector2(430, 730), "Eternal winter", Font, UpdateModificators);
		Elements.Add(eternalWinter);
		SetTooltipText(eternalWinter, "Brutal everlasting winter");
		WorldCreationBoolButtonPreset(eternalWinter); 
		
		_modificatorButtons.AddRange([lowGravity, highGravity, eternalWinter]);
		
		
		var createButton = new Button(new Vector2(950, 900), "Create", Font, Create)
		{
			Scale = new Vector2(1.2f, 1.2f)
		};
		Elements.Add(createButton);
		
		var back = new Button(new Vector2(850, 900), "Back", Font, Back)
		{
			Scale = new Vector2(1.2f, 1.2f)
		};
		Elements.Add(back);
		
		WorldCreationButtonPreset(createButton);
		WorldCreationButtonPreset(back);
		
		_errorText = new Text(new Vector2(0, 960), "World with same name already exists", Font) 
		{
		    HAlign = 0.5f,
		    DrawColorGetter = () => BlastiaGame.ErrorColor,
		    Alpha = 0f
		};
		Elements.Add(_errorText);
		
        // call an extra update for buttons to keep up
        base.Update();
	}

    protected override void OnMenuActive()
    {
        base.OnMenuActive();
        RandomizeWorldName();
        RandomizeSeed();
        
        ResetSettings();
    }
	
	/// <summary>
	/// Updates list of warnings
	/// </summary>
	private void UpdateModificators() 
	{
		if (_warnings == null) return;
		_warnings.ClearChildren();

		if (_modificatorButtons[0].GetState())
			_warnings.AddChild(new AnomalyUi(Vector2.Zero, "low gravity", Font));

		if (_modificatorButtons[1].GetState())
			_warnings.AddChild(new WarningUi(Vector2.Zero, "high gravity", Font));

		if (_modificatorButtons[2].GetState())
			_warnings.AddChild(new WarningUi(Vector2.Zero, "eternal winter", Font));
	}
    
    private void Back() 
    {
        SwitchToMenu(BlastiaGame.GetMenu<WorldSelectionMenu>());
    }
    
    private void RandomizeWorldName() => _name?.SetText(WorldNameGenerator.Generate(20));
    
    private void RandomizeSeed() 
    {
		if (_seed == null) return;
		
		int length = BlastiaGame.Rand.Next(15, 21);
		char[] digits = new char[length];
		for (int i = 0; i < length; i++) 
		{
		    digits[i] = (char) ('0' + BlastiaGame.Rand.Next(10));
		}
		
		_seed.SetText(new string(digits));
    }
    
    private void ResetSettings() 
    {
    	// reset all buttons
		if (!_difficultyButtons[1].GetState())
        	_difficultyButtons[1]?.OnClickChangeValue();
        	
        if (!_sizeButtons[1].GetState())
        	_sizeButtons[1]?.OnClickChangeValue();
        
        // clear modificators
        foreach (var button in _modificatorButtons)
            if (button.GetState())
				button.OnClickChangeValue();
		
        UpdateModificators();
    }

	protected void Create()
	{
		if (_name == null || _seed == null || string.IsNullOrEmpty(_seed.Text) || _errorText == null) return;
		
		(int width, int height) = GetSelectedSize();
		WorldDifficulty difficulty = GetSelectedDifficulty();
		Console.WriteLine($"[WORLD] Name: {_name.Text}, Seed: {_seed.Text}, World difficulty: {difficulty}, Width: {width}, Height: {height}");
		
		string name = _name.StringBuilder.ToString();
		BigInteger seed = BigInteger.Parse(_seed.StringBuilder.ToString());
		
		SaveValidationResult result = WorldManager.Instance.NewWorld(name, seed, difficulty, width, height);
		if (result == SaveValidationResult.Success) 
		{
			SwitchToMenu(BlastiaGame.GetMenu<WorldGenerationStatusMenu>());
		    return;
		}
		
		string message = result switch 
		{
		    SaveValidationResult.InvalidName => "Invalid characters in the name",
		    SaveValidationResult.InvalidPath => "Invalid world path. Please check the folder's name",
		    SaveValidationResult.AlreadyExists => "World with that name already exists",
		    _ => "Error while creating the world"
		};
		
		_errorText.Text = message;
		_errorText.Alpha = 2f;
		_errorText.LerpAlphaToZero = true;
	}
}