using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Buttons;

public class BoolSwitchButton : Button, IValueStorageUi<bool>
{
    public Func<bool>? GetValue { get; set; }
    public Action<bool>? SetValue { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="position"></param>
    /// <param name="text"></param>
    /// <param name="font"></param>
    /// <param name="onClick"></param>
    /// <param name="getValue">Get value lambda for the bool switch</param>
    /// <param name="setValue">Lambda for setting the switch value to new value</param>
    /// <param name="subscribeToEvent">Lambda which subscribes this <c>Action</c> to desired <c>Action</c></param>
    public BoolSwitchButton(Vector2 position, string text, SpriteFont font, Action onClick,
        Func<bool> getValue, Action<bool> setValue, Action<Action>? subscribeToEvent = null) : base(position, text, font, onClick)
    {
        GetValue = getValue;
        SetValue = setValue;

        OnClick += onClick;
        OnClick += OnClickChangeValue;
        Text = InitialText + $": {getValue()}";
        
        if (subscribeToEvent != null) subscribeToEvent(UpdateLabel);
    }

    private void OnClickChangeValue()
    {
        bool current = !GetValue();
        SetValue(current);
        
        Text = InitialText + $": {GetValue()}";
    }

    public void UpdateLabel()
    {
        Text = InitialText + $": {GetValue()}";
    }
    
}