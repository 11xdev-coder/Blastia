using System.Numerics;
using Blastia.Main.GameState;
using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI;

public class Menu
{
    private float _hAlignOffset;
    public float HAlignOffset
    {
        get => _hAlignOffset;
        set => Properties.OnValueChangedProperty(ref _hAlignOffset, value, OnAlignmentOffsetChanged);
    }
    
    private float _vAlignOffset;
    public float VAlignOffset
    {
        get => _vAlignOffset;
        set => Properties.OnValueChangedProperty(ref _vAlignOffset, value, OnAlignmentOffsetChanged);
    }
    
    public readonly List<UIElement> Elements = [];
    protected readonly SpriteFont Font;

    public bool Active;
    
    /// <summary>
    /// If <c>true</c> and player camera is initialized will use <see cref="Update(Camera)"/> to update.
    /// Otherwise, will use <see cref="Update()"/>
    /// </summary>
    public virtual bool CameraUpdate { get; set; }

    private bool _menuSwitched;

    protected Menu(SpriteFont font, bool isActive = false)
    {
        Font = font;
        Active = isActive;
        
        Initialize();
    }
    
    private void Initialize()
    {
        AddElements();
    }
    
    protected virtual void AddElements()
    {
        
    }

    /// <summary>
    /// Updates <c>AlignOffset</c> property in each element from <c>Elements</c>
    /// </summary>
    private void OnAlignmentOffsetChanged()
    {
        foreach (var element in Elements)
        {
            element.AlignOffset = new Vector2(HAlignOffset, VAlignOffset);
        }
    }
    
    /// <summary>
    /// Update each element
    /// </summary>
    public virtual void Update()
    {
        foreach (var elem in Elements)
        {
            elem.Update();
        }
    }

    /// <summary>
    /// Update each element
    /// </summary>
    /// <param name="playerCamera"></param>
    public virtual void Update(Camera playerCamera)
    {
        foreach (var elem in Elements)
        {
            elem.Update();
        }
    }
    
    /// <summary>
    /// Draw each element
    /// </summary>
    /// <param name="spriteBatch"></param>
    public virtual void Draw(SpriteBatch spriteBatch)
    {
        foreach (var elem in Elements)
        {
            elem.Draw(spriteBatch);
        }
    }
    
    /// <summary>
    /// Sets current menu to inactive and new menu to active
    /// </summary>
    /// <param name="menu"></param>
    protected void SwitchToMenu(Menu? menu)
    {
        if (menu != null && menu != this && !menu.Active)
        {
            OnMenuInactive();
            
            Active = false;
            menu.Active = true;
            menu.OnMenuActive();
            
            _menuSwitched = true;
        }
    }
    
    /// <summary>
    /// Called when SwitchToMenu is called on the new menu
    /// </summary>
    protected virtual void OnMenuActive()
    {
        
    }
    
    /// <summary>
    /// Invokes the OnMenuInactive method on all UI elements to handle their state when the menu becomes inactive.
    /// Outputs a debug message to indicate the method has been called.
    /// </summary>
    private void OnMenuInactive()
    {
        foreach (var elem in Elements)
        {
            elem.OnMenuInactive();
        }
        Console.WriteLine("Called OnMenuInactive on all UIElements.");
    }
    
    /// <summary>
    /// Runs in Game Update method to prevent other menus from updating when transitioning
    /// to new menu
    /// </summary>
    /// <returns></returns>
    public bool CheckAndResetMenuSwitchedFlag()
    {
        bool wasSwitched = _menuSwitched;
        _menuSwitched = false;
        return wasSwitched;
    }
    
    // COMMON METHODS
    private void AddSlider(Vector2 textPosition, Vector2 sliderPosition, 
        float hAlign, float vAlign, string text, Func<float> sliderGetValue,
        Action<float> sliderSetValue)
    {
        var textUi = new Text(textPosition, text, Font)
        {
            HAlign = hAlign - 0.13f,
            VAlign = vAlign
        };
        Elements.Add(textUi);
        
        var slider = new Slider(sliderPosition, Font,
            sliderGetValue, sliderSetValue, true)
        {
            HAlign = hAlign,
            VAlign = vAlign
        };
        Elements.Add(slider);
    }
    
    protected void AddMasterVolumeSlider(float hAlign, float vAlign) =>
        AddSlider(Vector2.Zero, Vector2.Zero, hAlign, vAlign, "Master Volume",
            () => AudioManager.Instance.MasterVolume, 
            f => AudioManager.Instance.MasterVolume = f);
    
    
    protected void AddMusicVolumeSlider(float hAlign, float vAlign) =>
        AddSlider(Vector2.Zero, Vector2.Zero, hAlign, vAlign, "Music Volume",
            () => AudioManager.Instance.MusicVolume, 
            f => AudioManager.Instance.MusicVolume = f);

    protected void AddSoundVolumeSlider(float hAlign, float vAlign) =>
        AddSlider(Vector2.Zero, Vector2.Zero, hAlign, vAlign, "Sound Volume",
            () => AudioManager.Instance.SoundsVolume, 
            f => AudioManager.Instance.SoundsVolume = f);

    protected void AddFullscreenSwitch(float hAlign, float vAlign, Action onClick)
    {
        BoolSwitchButton isFullScreenButton = new BoolSwitchButton(Vector2.Zero, "Full Screen", Font, 
            onClick, 
            () => VideoManager.Instance.IsFullScreen,
            _ => VideoManager.Instance.ToggleFullscreen())
        {
            HAlign = hAlign,
            VAlign = vAlign
        };
        Elements.Add(isFullScreenButton);
    }

    protected void AddResolutionHandler(float hAlign, float vAlign, Action onClick)
    {
        HandlerArrowButton<DisplayMode> resolutionSwitcher = new HandlerArrowButton<DisplayMode>(Vector2.Zero, "Resolution", Font,
            onClick, 10, VideoManager.Instance.ResolutionHandler)
        {
            HAlign = hAlign,
            VAlign = vAlign
        };
        resolutionSwitcher.AddToElements(Elements);

    }
}