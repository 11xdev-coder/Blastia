using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Buttons;

public class BoolSwitchButton : Button
{
    private Func<bool> _getValue;
    private Action<bool> _setValue;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="position"></param>
    /// <param name="text"></param>
    /// <param name="font"></param>
    /// <param name="onClick"></param>
    /// <param name="getValue">Get value lambda for the bool switch</param>
    /// <param name="setValue">Lambda for setting the switch value to new value</param>
    /// <param name="subscribeToEvent"></param>
    public BoolSwitchButton(Vector2 position, string text, SpriteFont font, Action onClick,
        Func<bool> getValue, Action<bool> setValue, Action<Action> subscribeToEvent) : base(position, text, font, onClick)
    {
        _getValue = getValue;
        _setValue = setValue;

        OnClick += OnClickChangeValue;
        Text = InitialText + $": {getValue()}";
        
        subscribeToEvent(UpdateLabel);
    }

    private void OnClickChangeValue()
    {
        bool current = !_getValue();
        // add bool value to the end of initial text
        Text = InitialText + $": {current}";
        
        _setValue(current);
    }

    private void UpdateLabel()
    {
        Text = InitialText + $": {_getValue()}";
    }
}