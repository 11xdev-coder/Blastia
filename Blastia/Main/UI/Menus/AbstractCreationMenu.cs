using System.Numerics;
using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.UI.Warnings;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI.Menus;

public abstract class AbstractCreationMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
	protected Input? _name;
	private Text? _tooltipText;
	private Text? _errorText;
	protected abstract Menu? PreviousMenu { get; }
	
	/// <summary>
    /// When hovered over the element sets tooltip text to provided value
    /// </summary>
	protected void SetTooltipText(UIElement elem, string text) 
	{	
		if (_tooltipText == null) return;
		
	    elem.OnStartHovering += () => { _tooltipText.Text = text; };
	    elem.OnEndHovering += () => { _tooltipText.Text = ""; };
	}
	
	/// <summary>
	/// Get the first selected button's index from a group
	/// </summary>
	protected int GetSelectedIndex(List<Button> group) => group.FindIndex(b => b.GetState());
	
	/// <summary>
    /// Finds the selected button from <c>group</c> and maps the button group index to index in <c>values</c>. List values must be in the same order with buttons for it to work.
    /// If none are selected returns <c>values[defaultIdx]</c>
    /// </summary>
	protected T GetValueFromSelectedButton<T>(List<Button> group, List<T> values, int defaultIdx = 1) 
	{
	    int idx = GetSelectedIndex(group);
	    return idx >= 0 ? values[idx] : values[defaultIdx];
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
		
		// --------------- NAME & SEED -----------------------
		var nameRandButton = new ImageButton(new Vector2(415, 317), BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Name", "UI", "WorldCreation"), new Vector2(2f, 2f)), Font, RandomizeName);
		Elements.Add(nameRandButton);
		SetTooltipText(nameRandButton, "Randomize name");
				
		WorldCreationButtonPreset(nameRandButton);
		
		_name = new Input(new Vector2(565, 315), Font, true, labelText: "Name", defaultText: "") 
		{
		    CharacterLimit = 20	    
		};
		_name.SetBackgroundProperties(_name.GetBackgroundBounds, Color.Black, 1, Color.Transparent, 5);
		Elements.Add(_name);		
		
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
		
		_errorText = new Text(new Vector2(0, 960), "B", Font) 
		{
		    HAlign = 0.5f,
		    DrawColorGetter = () => BlastiaGame.ErrorColor,
		    Alpha = 0f
		};
		Elements.Add(_errorText);
	}

    protected override void OnMenuActive()
    {
        base.OnMenuActive();
        RandomizeName();
    }
    
    private void Back() => SwitchToMenu(PreviousMenu);
    
    /// <summary>
    /// Method that randomizes name when menu becomes active. Set name text directly
    /// </summary>
    protected abstract void RandomizeName();

    /// <summary>
    /// Method that gathers the settings and creates the save file. If it fails, call <c>ShowErrorMessage</c> and pass the result there
    /// </summary>
    protected abstract void Create();
    protected void ShowErrorMessage(SaveValidationResult result) 
    {
        if (_errorText == null) return;
        
        string message = result switch 
		{
		    SaveValidationResult.InvalidName => "Invalid characters in the name",
		    SaveValidationResult.InvalidPath => "Invalid save path. Please check the folder's name",
		    SaveValidationResult.AlreadyExists => "Save with that name already exists",
		    _ => "Error while creating save"
		};
		
		_errorText.Text = message;
		_errorText.Alpha = 2f;
		_errorText.LerpAlphaToZero = true;
    }
}