using System.Numerics;
using Blastia.Main.GameState;
using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI;

public enum ActivationMethod 
{
    Default,
    OnlyInGame,
    OnlyInMenu,
    HideWhenInGame
}

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

    /// <summary>
    /// If true, will set <c>IsAnyBlockEscapeMenuActive</c> flag when this menu is active. Used for not opening/closing inventory when escape is pressed on this menu
    /// </summary>
    public virtual bool BlockEscape { get; set; }
    
    /// <summary>
    /// <para>Default -> default activation by hand</para>
    /// <para>OnlyInGame -> activation when loaded into the world, deactivation when in main menu</para>
    /// <para>OnlyInMenu -> activation when in main menu, deactivation when loaded into the world</para>
    /// <para>HideInGame -> only hide when loaded into the world/para>
    /// </summary>
    public virtual ActivationMethod ActivationType { get; set; }

    private bool _menuSwitched;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="font">Font that will be used by elements in this menu</param>
    /// <param name="isActive">Is menu active when the game just started?</param>
    /// <param name="initializeElementsImmediately">Should we run <c>AddElements</c> right away in the constructor?</param>
    protected Menu(SpriteFont font, bool isActive = false, bool initializeElementsImmediately = true)
    {
        Font = font;
        Active = isActive;
        
        Initialize(initializeElementsImmediately);
    }
    
    private void Initialize(bool initializeElementsImmediately)
    {
        if (initializeElementsImmediately)
        {
            AddElements();
        }
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
        Action<float> sliderSetValue, Action<Action> subscribeToEvent)
    {
        var textUi = new Text(textPosition, text, Font)
        {
            HAlign = hAlign - 0.13f,
            VAlign = vAlign
        };
        Elements.Add(textUi);
        
        var slider = new Slider(sliderPosition, Font,
            sliderGetValue, sliderSetValue, subscribeToEvent, true)
        {
            HAlign = hAlign,
            VAlign = vAlign
        };
        Elements.Add(slider);
    }
    
    protected void AddMasterVolumeSlider(float hAlign, float vAlign) =>
        AddSlider(Vector2.Zero, Vector2.Zero, hAlign, vAlign, "Master Volume",
            () => AudioManager.Instance.MasterVolume, 
            f => AudioManager.Instance.MasterVolume = f,
            handler => AudioManager.Instance.MasterVolumeChanged += handler);
    
    protected void AddMusicVolumeSlider(float hAlign, float vAlign) =>
        AddSlider(Vector2.Zero, Vector2.Zero, hAlign, vAlign, "Music Volume",
            () => AudioManager.Instance.MusicVolume, 
            f => AudioManager.Instance.MusicVolume = f,
            handler => AudioManager.Instance.MusicVolumeChanged += handler);

    protected void AddSoundVolumeSlider(float hAlign, float vAlign) =>
        AddSlider(Vector2.Zero, Vector2.Zero, hAlign, vAlign, "Sound Volume",
            () => AudioManager.Instance.SoundsVolume, 
            f => AudioManager.Instance.SoundsVolume = f,
            handler => AudioManager.Instance.SoundsVolumeChanged += handler);

    protected void AddFullscreenSwitch(float hAlign, float vAlign, Action onClick)
    {
        var isFullScreenButton = new Button(Vector2.Zero, "Full Screen", Font, onClick)
        {
            HAlign = hAlign,
            VAlign = vAlign
        };
        isFullScreenButton.CreateBooleanSwitch( 
            () => VideoManager.Instance.IsFullScreen,
            _ => VideoManager.Instance.ToggleFullscreen(),
            handler => VideoManager.Instance.FullScreenChanged += handler);
        Elements.Add(isFullScreenButton);
    }

    protected void AddResolutionHandler(float hAlign, float vAlign, Action onClick)
    {
        HandlerArrowButton<DisplayMode> resolutionSwitcher = new HandlerArrowButton<DisplayMode>(Vector2.Zero, "Resolution", Font,
            onClick, 10, VideoManager.Instance.ResolutionHandler,
            handler => VideoManager.Instance.ResolutionChanged += handler)
        {
            HAlign = hAlign,
            VAlign = vAlign
        };
        resolutionSwitcher.AddToElements(Elements);

    }
    
    protected void WorldCreationBoolButtonPreset(Button button, List<Func<Button>>? buttonGroupGetters = null) 
    {
        button.CreateBooleanSwitch(null, null, null, false, (newVal, button) => 
        {
            if (newVal)
                button.SetBackgroundColor(Color.Yellow);
            else
                button.RevertOriginalBackgroundColor();
        }, buttonGroupGetters);
    }
}