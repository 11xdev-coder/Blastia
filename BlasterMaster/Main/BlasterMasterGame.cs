using System.Diagnostics;
using BlasterMaster.Main.Sounds;
using BlasterMaster.Main.UI;
using BlasterMaster.Main.UI.Menus;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BlasterMaster.Main;

public class BlasterMasterGame : Game
{
    /// <summary>
    /// Graphics manages the graphics device (connection between game and graphics hardware).
    /// Manages things such as resolution, VSync, fullscreen or windowed mode, etc.
    /// Used for low-level graphics operations.
    /// </summary>
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public static float ScreenWidth { get; private set; }
    public static float ScreenHeight { get; private set; }
    public static bool HasClickedLeft { get; private set; }
    public static Vector2 CursorPosition { get; private set; }
    public static bool IsHoldingLeft { get; private set; }

    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    private Texture2D _cursorTexture;
    
    public SpriteFont MainFont { get; private set; }
    private Texture2D _logoTexture;

    public static LogoMenu? LogoMenu { get; private set; }
    public static MainMenu? MainMenu { get; private set; }
    public static SettingsMenu? SettingsMenu { get; private set; }
    public static AudioSettingsMenu? AudioSettingsMenu { get; private set; }
    public static VideoSettingsMenu? VideoSettingsMenu { get; private set; }
    private readonly List<Menu> _menus;

    /// <summary>
    /// Event triggered when a request to exit the game is made.
    /// This event allows for a clean shutdown process by ensuring
    /// all necessary operations are completed prior to exiting.
    /// </summary>
    private static event Action? ExitRequestEvent;
    
    /// <summary>
    /// Event triggered when a request to update resolution is made.
    /// This event allows for ScreenWidth and ScreenHeight values to
    /// properly update when resolution has changed.
    /// </summary>
    private static event Action? ResolutionRequestEvent;
    
    public BlasterMasterGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _menus = new List<Menu>();
        
        // root folder of all assets
        Content.RootDirectory = "Main/Content";
        SoundEngine.Initialize(Content);
        
        // initialize video manager with graphics manager
        string videoManagerPath = Content.RootDirectory + Paths.VideoManagerSavePath;
        VideoManager.Instance.Initialize(_graphics, videoManagerPath);
        VideoManager.Instance.LoadStateFromFile();
        // load audio manager
        string audioManagerPath = Content.RootDirectory + Paths.AudioManagerSavePath;
        AudioManager.Instance.Initialize(audioManagerPath);
        AudioManager.Instance.LoadStateFromFile();
        
        ExitRequestEvent += OnExitRequested;
        ResolutionRequestEvent += UpdateResolution;
    }

    protected override void Initialize()
    {
        base.Initialize();

        int width = VideoManager.Instance.ResolutionHandler.GetCurrentResolutionWidth();
        int height = VideoManager.Instance.ResolutionHandler.GetCurrentResolutionHeight();
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();
        
        UpdateResolution();
    }
    
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        SoundEngine.LoadSounds();
        
        _cursorTexture = LoadingUtilities.LoadTexture(GraphicsDevice, 
            Content.RootDirectory + Paths.CursorTexturePath);

        MainFont = Content.Load<SpriteFont>("Font/Andy_24_Regular");
        _logoTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.Logo5XTexturePath);
        
        LogoMenu = new LogoMenu(MainFont, _logoTexture);
        AddMenu(LogoMenu);
        
        MainMenu = new MainMenu(MainFont);
        AddMenu(MainMenu);
        
        SettingsMenu = new SettingsMenu(MainFont);
        AddMenu(SettingsMenu);

        AudioSettingsMenu = new AudioSettingsMenu(MainFont);
        AddMenu(AudioSettingsMenu);
        
        VideoSettingsMenu = new VideoSettingsMenu(MainFont);
        AddMenu(VideoSettingsMenu);
    }

    protected override void UnloadContent()
    {
        base.UnloadContent();
        SoundEngine.UnloadSounds();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        UpdateMouseState();

        foreach (Menu menu in _menus)
        {
            if (menu.Active)
            {
                menu.Update();
                // prevent new menu from updating when switched
                if (menu.CheckAndResetMenuSwitchedFlag())
                {
                    break;
                }
            }
        }
    }

    private void UpdateMouseState()
    {
        _currentMouseState = Mouse.GetState();
        
        HasClickedLeft = _currentMouseState.LeftButton == ButtonState.Released 
                         && _previousMouseState.LeftButton == ButtonState.Pressed;

        IsHoldingLeft = _currentMouseState.LeftButton == ButtonState.Pressed;
        
        // mouse position that is aligned with OS cursor
        CursorPosition = Vector2.Transform(new Vector2(_currentMouseState.X, _currentMouseState.Y),
            Matrix.Invert(VideoManager.Instance.CalculateResolutionScaleMatrix()));
        
        // set previous in the end
        _previousMouseState = _currentMouseState;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        var matrix = VideoManager.Instance.CalculateResolutionScaleMatrix();
        
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, 
            null, matrix);
        
        foreach (Menu menu in _menus)
        {
            if (menu.Active)
            {
                menu.Draw(_spriteBatch);
            }
        }
        
        // draw cursor texture last on top of everything
        _spriteBatch.Draw(_cursorTexture, CursorPosition, Color.White);
        _spriteBatch.End();
    }

    public static void RequestResolutionUpdate()
    {
        ResolutionRequestEvent?.Invoke();
    }

    private void UpdateResolution()
    {
        ScreenWidth = GraphicsDevice.Viewport.Width;
        ScreenHeight = GraphicsDevice.Viewport.Height;
    }

    private void AddMenu(Menu? menu)
    {
        if(menu != null) _menus.Add(menu);
    }

    private void OnExitRequested()
    {
        Exit();
    }

    public static void RequestExit()
    {
        ExitRequestEvent?.Invoke();
    }
}