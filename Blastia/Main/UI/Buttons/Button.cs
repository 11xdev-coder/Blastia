using Blastia.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Buttons;

public class Button : UIElement, IValueStorageUi<bool>
{
    public Func<bool>? GetValue { get; set; }
    public Action<bool>? SetValue { get; set; }
    
    public Color NormalColor = Color.White;
    public Color SelectedColor = Color.Yellow;
    private ColoredBackground? _background;
    private bool _hasBackground;
    private Color _backgroundColor;
    private float _borderThickness;
    private Color _borderColor;
    private float _padding;
    
    /// <summary>
    /// Called whenever boolean value changed
    /// </summary>
    private Action? _onValueChangedUpdate;
    /// <summary>
    /// Used when creating boolean buttons <c>GetValue</c> or <c>SetValue</c> are null
    /// </summary>
    private bool _state;

    public Button(Vector2 position, string text, SpriteFont font, Action? onClick) : 
        base(position, text, font)
    {
        OnClick += onClick;
        
        DrawColor = NormalColor;
        
        OnStartHovering = () => { PlayTickSound(); Select(); };
        OnEndHovering = Deselect;
    }
    
    /// <summary>
    /// Creates a button with custom background
    /// </summary>
    public Button(Vector2 position, string text, SpriteFont font, Action? onClick, Color backgroundColor, float borderThickness, Color borderColor, float padding) : 
        this(position, text, font, onClick)
    {
        _hasBackground = true;
        _backgroundColor = backgroundColor;
        _borderThickness = borderThickness;
        _borderColor = borderColor;
        _padding = padding;
    }
    
    /// <summary>
    /// Turns this button into a boolean switch
    /// </summary>
    /// <param name="getValue">Original value getter</param>
    /// <param name="setValue">Method for setting the original value to new value</param>
    /// <param name="whenOriginalValueChanged">Method that subscribes handler to when original value changes. Used when original value changes from other source to update this value</param>
    /// <param name="showValue">Will show the value of the boolean (e.g. "Test: True")</param>
    /// <param name="getValue">Executed whenever the button is pressed (or original value changed). Boolean argument is the new value</param>
    public void CreateBooleanSwitch(Func<bool>? getValue, Action<bool>? setValue, Action<Action>? whenOriginalValueChanged, bool showValue = true, Action<bool, Button>? onValueChanged = null) 
    {
        GetValue = getValue == null ? () => _state : getValue;
        SetValue = setValue == null ? (val) => _state = val : setValue;
        
        Action updateAction = () => 
        {
            if (GetValue == null) return;
            
            if (showValue)
                Text = $"{InitialText}: {GetValue()}";
            
            // call update with the new variable
            onValueChanged?.Invoke(GetValue(), this);
        };
        _onValueChangedUpdate = updateAction;
        
        OnClick += OnClickChangeValue;
        updateAction();
        
        if (whenOriginalValueChanged != null)
            whenOriginalValueChanged(updateAction);
    }
    
    private void OnClickChangeValue() 
    {
        if (GetValue == null || SetValue == null) return;
        
        // get opposite value and set it
        var opposite = !GetValue();
        SetValue(opposite);
        
        _onValueChangedUpdate?.Invoke();
    }

    public override void OnAlignmentChanged()
    {
        base.OnAlignmentChanged();
        
        if (_background == null) return;
        _background.Position = new Vector2(Bounds.Left - _padding, Bounds.Top - _padding);
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        
        if (_hasBackground && _background == null) 
        {
            _background = new ColoredBackground(new Vector2(Bounds.Left - _padding, Bounds.Top - _padding), Bounds.Width + _padding * 2, Bounds.Height + _padding * 2, _backgroundColor, _borderThickness, _borderColor);
        }
    }
    
    public void SetBackgroundColor(Color newColor) => _background?.SetBackgroundColor(newColor);
    public void RevertOriginalBackgroundColor() => _background?.SetBackgroundColor(_backgroundColor);

    private void Select()
    {
        if (_hasBackground)
            _background?.SetBorderColor(SelectedColor);
        else
            DrawColor = SelectedColor;
    }

    private void Deselect()
    {
        _background?.SetBorderColor(_borderColor);
        DrawColor = NormalColor;
    }

    public override void Update()
    {
        _background?.Update();
        base.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        _background?.Draw(spriteBatch);
        base.Draw(spriteBatch);
    }

    public void UpdateLabel()
    {
        throw new NotImplementedException();
    }

    void IValueStorageUi<bool>.UpdateLabel()
    {
        UpdateLabel();
    }
}