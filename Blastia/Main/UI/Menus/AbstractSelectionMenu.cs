using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.UI.Menus.SinglePlayer;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus;

public abstract class AbstractSelectionItem<T> : UIElement where T : State
{
	private const int Width = 1200;
	private const int Height = 150;
	
	public readonly T State;
	
	private Text? _nameText;
	private Text? _metaText;
	private Button? _playButton;
	
	public bool IsSelected { get; set; }
	
	/// <summary>
    /// Adds elements to the item. Add your own constructor to add items
    /// </summary>
    public AbstractSelectionItem(Vector2 position, T state, SpriteFont font) : base(position, "", font)
	{
		State = state;
		
		SetBackgroundProperties(() => new Rectangle(Bounds.Left, Bounds.Top, Width, Height), Colors.SelectionItemBg, 2, Colors.SelectionItemBorder, 0);
		
		_nameText = new Text(Vector2.Zero, State.Name, font) 
		{
		    DrawColor = Colors.SelectionItemTextDim,
		    BorderColor = new(0, 0, 0, 0)
		};
		ChildElements.Add(_nameText);
		
		_metaText = new Text(Vector2.Zero, $"99h 59m 59s | {State.CreatedAtDisplay}", font) 
		{
		    Scale = new Vector2(0.7f, 0.7f),
		    DrawColor = Colors.SelectionItemMetaDim,
		    BorderColor = new(0, 0, 0, 0)
		};
		ChildElements.Add(_metaText);
		
		_playButton = new ImageButton(Vector2.Zero, BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("PlayButton", "UI", "WorldSelection"), new Vector2(3f, 3f)), font, () => {}) 
		{
		    DrawColor = Colors.SelectionItemBorder,
		    OnStartHovering = () => {},
		    OnEndHovering = () => {},
		};
		_playButton.OnClick += LoadState;
		ChildElements.Add(_playButton);
	}
    
	protected abstract void LoadState();
	
	public override void Update() 
	{
	    base.Update();
	    
	    Background?.SetBackgroundColor(IsHovered || IsSelected ? Colors.SelectionItemBgSelected : Colors.SelectionItemBg);
        Background?.SetOutlineColor(IsHovered || IsSelected ? Colors.DimmedGold : Colors.SelectionItemBorder);
        ChangeDrawColor(_nameText, IsHovered || IsSelected ? Colors.SelectionItemText : Colors.SelectionItemTextDim);
        ChangeDrawColor(_metaText, IsHovered || IsSelected ? Colors.SelectionItemMeta : Colors.SelectionItemMetaDim);
        ChangeDrawColor(_playButton, IsHovered || IsSelected ? Colors.DimmedGold : Colors.SelectionItemBorder);
	}

    public override void UpdateBounds()
    {		
		// background may be null here so we must update bounds regardless
		
		UpdateBoundsBase(Width, Height);
		UpdateChildrenPositions();
   }
    
    /// <summary>
    /// Update's children positions every frame. Override to add your own
    /// </summary>
    protected virtual void UpdateChildrenPositions() 
    {
		if (_nameText != null)
        	_nameText.Position = new Vector2(Bounds.Left + 150, Bounds.Top + 10);
        	
        if (_metaText != null)
			_metaText.Position = new Vector2(Bounds.Left + 150, Bounds.Top + 50);
			
		if (_playButton != null)
			_playButton.Position = new Vector2(Bounds.Right - _playButton.Bounds.Width - 20, Bounds.Top + Height * 0.5f - _playButton.Bounds.Height * 0.5f);
    }
    
    /// <summary>
	/// Automatically checks element for null and changes its <c>DrawColor</c>. Helper method to save 1 line
	/// </summary>
    protected void ChangeDrawColor(UIElement? e, Color newCol) 
    {
        if (e == null) return;
        e.DrawColor = newCol;
    }
}

public abstract class AbstractSelectionMenu<T> : Menu where T : State
{
	public override ActivationMethod ActivationType => ActivationMethod.HideWhenInGame;
	
	protected virtual string TopText { get; set; } = "";
	private List<T> _states = [];
	private ScrollableArea? _area;
	private Text? _amountText;
	private Text? _deleteConfirmation;
	private Button? _deleteYes;
	private Button? _deleteNo;
	
	protected abstract Menu? CreationMenu { get; }
	protected abstract Menu? PreviousMenu { get; }
	
	protected abstract List<T> GetStates();
	
	/// <summary>
    /// Method that creates a child of abstract SelectionItem with provided state
    /// </summary>
	protected abstract AbstractSelectionItem<T> CreateSelectionItem(T state);

	public AbstractSelectionMenu(SpriteFont font, bool isActive = false) : base(font, isActive, false) 
	{
	    _states = GetStates();
	    AddElements();
	}
	
	protected override void OnMenuActive() => UpdateUi();
	
	private void UpdateUi() 
	{
	    _states = GetStates();
		_area?.ClearChildren();
		
	    foreach (var state in _states) 
		{
		    AbstractSelectionItem<T> item = CreateSelectionItem(state);
		    item.OnClick += () => HighlightSelectionItem(item);
		    _area?.AddChild(item);
		}
		
		if (_amountText != null)
			_amountText.Text = $"{_states.Count} items";
	}

    protected override void AddElements()
	{
		Vector2 s = Font.MeasureString(TopText);
		Text worldSelectText = new Text(new Vector2(-650 + s.X * 0.5f, -300 - s.Y), TopText, Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.59f
		};
		Elements.Add(worldSelectText);
		
		string text = $"{_states.Count} items";
		s = Font.MeasureString(text);
		_amountText = new Text(new Vector2(650 - s.X * 0.5f, -300 - s.Y), text, Font) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.59f
		};
		Elements.Add(_amountText);
		
		AdvancedBackground bg = new AdvancedBackground(Vector2.Zero, 1300, 600, Colors.DarkBackground, 2, Colors.DarkBorder) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(bg);
		
		Viewport viewport = new Viewport(1300, 580);
		_area = new ScrollableArea(Vector2.Zero, viewport, spacing: 10) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(_area);
		
		Vector2 createScale = Font.MeasureString("Create");
		Vector2 deleteScale = Font.MeasureString("Delete");
		Vector2 backScale = Font.MeasureString("Back");
		float spacing = 15f;
		Button createButton = new Button(new Vector2(-(deleteScale.X + createScale.X) * 0.5f - spacing, 250), "Create", Font, () => SwitchToMenu(CreationMenu)) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(createButton);
		
		Button deleteButton = new Button(new Vector2(0, 250), "Delete", Font, ShowDeleteConfirmation) 
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(deleteButton);
		
		Button back = new Button(new Vector2((deleteScale.X + backScale.X) * 0.5f + spacing, 250), "Back", Font, () => SwitchToMenu(PreviousMenu))
		{
		    HAlign = 0.5f,
		    VAlign = 0.65f
		};
		Elements.Add(back);
		
		_deleteConfirmation = new Text(new Vector2(0, 250 + deleteScale.Y + 20), "", Font) 
		{
			HAlign = 0.5f,
			VAlign = 0.65f,
		    Alpha = 0f
		};
		Elements.Add(_deleteConfirmation);
		
		_deleteYes = new Button(new Vector2(0, 250 + deleteScale.Y + 20), "Yes", Font, DeleteYes) 
		{
			HAlign = 0.5f,
			VAlign = 0.65f,
		    Alpha = 0f,
		    DrawColor = Color.LimeGreen,
		    NormalColor = Color.LimeGreen,
			SelectedColor = Color.LimeGreen
		};
		Elements.Add(_deleteYes);
		
		_deleteNo = new Button(new Vector2(0, 250 + deleteScale.Y + 20), "No", Font, DeleteNo) 
		{
			HAlign = 0.5f,
			VAlign = 0.65f,
		    Alpha = 0f,
		    DrawColor = Color.DarkRed,
		    NormalColor = Color.DarkRed,
			SelectedColor = Color.DarkRed
		};
		Elements.Add(_deleteNo);
		
		WorldCreationButtonPreset(createButton);
		WorldCreationButtonPreset(deleteButton);
		WorldCreationButtonPreset(back);
	}
	
	private void HighlightSelectionItem(AbstractSelectionItem<T> item) 
	{
	    if (_area == null) return;
	    
	    foreach (var ui in _area.Children) 
	    {
			if (ui is not AbstractSelectionItem<T> selectionItem)
				continue;
				
			selectionItem.IsSelected = selectionItem == item;
	    }
	}
	
	private AbstractSelectionItem<T>? GetSelectedItem() => _area?.Children.OfType<AbstractSelectionItem<T>>().FirstOrDefault(s => s.IsSelected);
	
	private void ShowDeleteConfirmation() 
	{
		var selectedItem = GetSelectedItem();
	    if (selectedItem == null || _deleteConfirmation == null || _deleteYes == null || _deleteNo == null) return;
	    
	    string text = $"Delete {selectedItem.State.Name}?";
		Vector2 size = Font.MeasureString(text);
		_deleteConfirmation.Text = text;
		_deleteConfirmation.Position.X = -size.X;
		_deleteConfirmation.Alpha = 1f;
		
		Vector2 yesSize = Font.MeasureString("Yes");
		Vector2 noSize = Font.MeasureString("No");
		_deleteYes.Position.X = yesSize.X;
		_deleteYes.Alpha = 1f;
		_deleteNo.Position.X = yesSize.X + noSize.X + 20;
		_deleteNo.Alpha = 1f;
	}
	
	private void HideDeleteConfirmation() 
	{
		if (_deleteConfirmation == null || _deleteYes == null || _deleteNo == null) return;
		
	    _deleteConfirmation.Alpha = 0f;
	    _deleteYes.Alpha = 0f;
	    _deleteNo.Alpha = 0f;
	}
	
	protected abstract bool DeleteState(string filePath);
	
	private void DeleteYes() 
	{
	    var selectedItem = GetSelectedItem();
	    if (selectedItem == null) return;
	    
	    HideDeleteConfirmation();
	    
	    bool deleted = DeleteState(selectedItem.State.FilePath);
	    if (deleted)
	    	UpdateUi();
	    else
	    	Console.WriteLine("[AbstractSelectionMenu] Error while deleting");
	}
	
	private void DeleteNo() => HideDeleteConfirmation();	
}