using Blastia.Main.UI.Buttons;
using Blastia.Main.UI.Warnings;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Blastia.Main.UI.Menus.SinglePlayer;

public enum WorldModificator 
{
    LowGravity,
    HighGravity,
    EternalWinter
}


public class WorldCreationMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
	private Image? _worldPreview;
	private Image? _worldPreviewBorder;
	private Input? _name;
	private Input? _seed;
	private ScrollableArea? _warnings;
	
	private bool _lowGraivty;
	private bool _highGravity;
	private bool _eternalWinter;
	
	protected override void AddElements()
	{
		AdvancedBackground bg = new AdvancedBackground(Vector2.Zero, 1400, 600, Colors.DarkBackground, 2, Colors.DarkBorder)
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
		
		// --------------- NAME & SEED -----------------------
		var nameRandButton = new ImageButton(new Vector2(415, 317), BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Name", "UI", "WorldCreation"), new Vector2(2f, 2f)), Font, RandomizeWorldName);
		Elements.Add(nameRandButton);
		
		var seedRandButton = new ImageButton(new Vector2(415, 377), BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Seed", "UI", "WorldCreation"), new Vector2(2f, 2f)), Font, RandomizeSeed);
		Elements.Add(seedRandButton);
				
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
				
		var xl = new Button(Vector2.Zero, "XL", Font, () => {}) 
		{
		    HAlign = 0.375f,
		    VAlign = 0.44f
		};		
		Elements.Add(xl);
		
		WorldCreationBoolButtonPreset(small, [() => medium, () => large, () => xl]);
		WorldCreationBoolButtonPreset(medium, [() => small, () => large, () => xl]);
		WorldCreationBoolButtonPreset(large, [() => small, () => medium, () => xl]);
		WorldCreationBoolButtonPreset(xl, [() => small, () => medium, () => large]);
		
		// --------------- DIFFICULTY -----------------------
		var difficultyText = new Text(new Vector2(275, 550), "Difficulty", Font);
		Elements.Add(difficultyText);
		
		var easy = new Button(new Vector2(430, 550), "I am too young to die", Font, () => {});
		Elements.Add(easy);
		
		var normal = new Button(new Vector2(430, 605), "Hurt me plenty", Font, () => {});
		Elements.Add(normal);
		
		var hard = new Button(new Vector2(670, 605), "Nightmare", Font, () => {});
		Elements.Add(hard);
		
		WorldCreationBoolButtonPreset(easy, [() => normal, () => hard]);
		WorldCreationBoolButtonPreset(normal, [() => easy, () => hard]);
		WorldCreationBoolButtonPreset(hard, [() => easy, () => normal]);
		
		// --------------- WARNINGS -----------------------
		var viewport = new Viewport(400, 500);
		_warnings = new ScrollableArea(new Vector2(1180, 330), viewport);
		Elements.Add(_warnings);
		
		// --------------- MODIFICATORS -----------------------
		var lowGravity = new Button(new Vector2(430, 680), "Low gravity", Font, () => OnModificatorClick(WorldModificator.LowGravity));
		Elements.Add(lowGravity);
		var highGravity = new Button(new Vector2(620, 680), "High gravity", Font, () => OnModificatorClick(WorldModificator.HighGravity));
		Elements.Add(highGravity);
		WorldCreationBoolButtonPreset(lowGravity, [() => highGravity]);
		WorldCreationBoolButtonPreset(highGravity, [() => lowGravity]);
		
		var eternalWinter = new Button(new Vector2(430, 730), "Eternal winter", Font, () => OnModificatorClick(WorldModificator.EternalWinter));
		Elements.Add(eternalWinter);
		WorldCreationBoolButtonPreset(eternalWinter); 
		
		
		var createButton = new Button(new Vector2(950, 900), "Create", Font, () => {})
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
	}

    protected override void OnMenuActive()
    {
        base.OnMenuActive();
        RandomizeWorldName();
        RandomizeSeed();
    }
    
    private void OnModificatorClick(WorldModificator mod) 
    {
		if (_warnings == null) return;
		
        switch (mod)
        {
            case WorldModificator.LowGravity:
				_lowGraivty = !_lowGraivty;
				break;
			case WorldModificator.HighGravity:
				_highGravity = !_highGravity;
				break;
			case WorldModificator.EternalWinter:
				_eternalWinter = !_eternalWinter;
				break;
        }
        
        if (_lowGraivty) 
        {
            var anomaly = new AnomalyUi(Vector2.Zero, "Low gravity", Font);
            _warnings.AddChild(anomaly);
        }
        else 
        {
            _warnings.RemoveByIndex(0);
        }
        
        if (_highGravity) 
        {
            var w = new WarningUi(Vector2.Zero, "High gravity", Font);
            _warnings.AddChild(w);
        }
        else 
        {
            _warnings.RemoveByIndex(1);
        }
        
        if (_eternalWinter) 
        {
            var w = new WarningUi(Vector2.Zero, "Eternal Winter", Font);
            _warnings.AddChild(w);
        }
        else 
        {
            _warnings.RemoveByIndex(2);
        }
    }

    public override void Update()
    {
        base.Update();
        if (_worldPreview == null) return;
    }
    
    private void Back() 
    {
        SwitchToMenu(BlastiaGame.WorldsMenu);
    }
    
    private void RandomizeWorldName() 
    {
		if (_name == null) return;
		
        _name.SetText(WorldNameGenerator.Generate(20));
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