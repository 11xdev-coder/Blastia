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
    
    
    /// <summary>
    /// Called whenever boolean value changed
    /// </summary>
    private Action? _onValueChangedUpdate;
    /// <summary>
    /// Used when creating boolean buttons <c>GetValue</c> or <c>SetValue</c> are null
    /// </summary>
    private bool _state;
    /// <summary>
    /// List of button getters that will deactivate once this button is activated (when this is a boolean switch)
    /// </summary>
    private List<Func<Button>> _buttonGroup = [];

    public Button(Vector2 position, string text, SpriteFont font, Action? onClick) : 
        base(position, text, font)
    {
        OnClick += onClick;
        
        DrawColor = NormalColor;
        
        OnStartHovering = () => { PlayTickSound(); Select(); };
        OnEndHovering = Deselect;
    }
    
    /// <summary>
    /// Turns this button into a boolean switch
    /// </summary>
    /// <param name="getValue">Original value getter</param>
    /// <param name="setValue">Method for setting the original value to new value</param>
    /// <param name="whenOriginalValueChanged">Method that subscribes handler to when original value changes. Used when original value changes from other source to update this value</param>
    /// <param name="showValue">Will show the value of the boolean (e.g. "Test: True")</param>
    /// <param name="getValue">Executed whenever the button is pressed (or original value changed). Boolean argument is the new value</param>
    public void CreateBooleanSwitch(Func<bool>? getValue, Action<bool>? setValue, Action<Action>? whenOriginalValueChanged, bool showValue = true, Action<bool, Button>? onValueChanged = null,
        List<Func<Button>>? buttonGroupGetters = null) 
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
        
        if (buttonGroupGetters == null) return;
        foreach (var buttonGetter in buttonGroupGetters)
            _buttonGroup.Add(buttonGetter);
    }
    
    /// <summary>
    /// Just sets the opposite value for this button
    /// </summary>
    public void SetOppositeValue() 
    {
        if (GetValue == null || SetValue == null) return;
        
        // get opposite value and set it
        var opposite = !GetValue();
        SetValue(opposite);
        
        _onValueChangedUpdate?.Invoke();
    }
    
    /// <summary>
    /// Called when clicked directly on this button => updates value and deselects other buttons
    /// </summary>
    private void OnClickChangeValue() 
    {
        SetOppositeValue();
        
        // deactivate other buttons
        foreach (var buttonGetter in _buttonGroup) 
        {
            var button = buttonGetter();
            if (button == this) continue;
            
            // opposite values for other buttons
            if (button.GetValue?.Invoke() == true) 
                button.SetOppositeValue();
        }
    }
    
    private void Select()
    {
        if (Background != null)
            Background?.SetBorderColor(SelectedColor);
        else
            DrawColor = SelectedColor;
    }

    private void Deselect()
    {
        Background?.SetBorderColor(OriginalBorderColor);
        DrawColor = NormalColor;
    }

    public override void Update()
    {
        Background?.Update();
        base.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Background?.Draw(spriteBatch);
        base.Draw(spriteBatch);
    }

    public void UpdateLabel()
    {
        _onValueChangedUpdate?.Invoke();
    }
}