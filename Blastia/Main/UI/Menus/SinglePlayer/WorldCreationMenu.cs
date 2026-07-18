using System.Numerics;
using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.UI.Warnings;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldCreationMenu : AbstractCreationMenu
{
	private Image? _worldPreview;
	private Image? _worldPreviewBorder;
	private Input? _seed;
	private ScrollableArea? _warnings;
	
	private List<Button> _sizeButtons = [];
	// sizes in order to match their buttons
	public static readonly List<(int width, int height)> SizeValues = [
		(4200, 1200),
		(6400, 1800),
		(8400, 2400),
		(16800, 4800)
	];
	private List<Button> _difficultyButtons = [];
	// difficulties in order to match their buttons
	private static readonly List<WorldDifficulty> DifficultyValues = [
		WorldDifficulty.Easy,
		WorldDifficulty.Medium,
		WorldDifficulty.Hard
	];
	private List<Button> _modificatorButtons = [];

    protected override Menu? PreviousMenu => BlastiaGame.GetMenu<WorldSelectionMenu>();

    public WorldCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
    }
	private (int width, int height) GetSelectedSize() => GetValueFromSelectedButton(_sizeButtons, SizeValues);
	
	private WorldDifficulty GetSelectedDifficulty() => GetValueFromSelectedButton(_difficultyButtons, DifficultyValues);
	
	protected override void AddElements()
	{
		base.AddElements();
				
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
		var seedRandButton = new ImageButton(new Vector2(415, 377), BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Seed", "UI", "WorldCreation"), new Vector2(2f, 2f)), Font, RandomizeSeed);
		Elements.Add(seedRandButton);
		SetTooltipText(seedRandButton, "Randomize seed");
		WorldCreationButtonPreset(seedRandButton);
		
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
		
		var easyScale = Font.MeasureString("Peaceful");
		var normalScale = Font.MeasureString("Unforgiving");
		var normal = new Button(new Vector2(430 + easyScale.X + 16, 550), "Unforgiving", Font, () => {});
		Elements.Add(normal);
		SetTooltipText(normal, "Greater difficulty with better loot");
		
		var hard = new Button(new Vector2(430 + easyScale.X + 16 + normalScale.X + 16, 550), "No mercy", Font, () => {});
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
		
        // call an extra update for buttons to keep up
        base.Update();
	}

    protected override void OnMenuActive()
    {
        base.OnMenuActive();
        RandomizeSeed();        
        ResetSettings();
    }

    protected override void RandomizeName(Input? name) => name?.SetText(RandomNameGenerator.Generate(name.CharacterLimit));
	
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
    
    protected override void Create() 
    {
        if (_name == null || _seed == null || string.IsNullOrEmpty(_seed.Text)) return;
		
		(int width, int height) = GetSelectedSize();
		WorldDifficulty difficulty = GetSelectedDifficulty();
		Console.WriteLine($"[WorldCreation] Name: {_name.Text}, Seed: {_seed.Text}, World difficulty: {difficulty}, Width: {width}, Height: {height}");
		
		string name = _name.StringBuilder.ToString();
		BigInteger seed = BigInteger.Parse(_seed.StringBuilder.ToString());
		
		SaveValidationResult result = WorldManager.Instance.NewWorld(name, seed, difficulty, width, height);
		if (result == SaveValidationResult.Success) 
		{
			SwitchToMenu(BlastiaGame.GetMenu<WorldGenerationStatusMenu>());
		    return;
		}
		
		ShowErrorMessage(result);
	}
}