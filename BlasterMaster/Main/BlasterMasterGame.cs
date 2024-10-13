using BlasterMaster.Main.Sounds;
using BlasterMaster.Main.UI;
using BlasterMaster.Main.UI.Menus;
using BlasterMaster.Main.UI.Menus.Settings;
using BlasterMaster.Main.UI.Menus.SinglePlayer;
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
    public static SpriteBatch SpriteBatch { get; private set; } = null!;
    
    // TICK
    public static GameTime GameTime { get; private set; } = new();
    public static double GameTimeElapsedSeconds { get; private set; }
    
    // SCREEN
    public static float ScreenWidth { get; private set; }
    public static float ScreenHeight { get; private set; }
    
    // MOUSE
    public static bool HasClickedLeft { get; private set; }
    public static Vector2 CursorPosition { get; private set; }
    public static bool IsHoldingLeft { get; private set; }
    public static float ScrollWheelDelta { get; private set; }
    
    // KEYBOARD
    public static KeyboardState KeyboardState { get; private set; }
    public static KeyboardState PreviousKeyboardState { get; private set; }
    
    // GLOBAL TEXTURES
    public static Texture2D CursorTexture { get; private set; } = null!;
    public static Texture2D SliderTexture { get; private set; } = null!;
    public static Texture2D WhitePixel { get; private set; } = null!;
    public static Texture2D InvisibleTexture { get; private set; } = null!;
    
    #region Player Textures

    public static Texture2D PlayerHead { get; private set; } = null!;
    public static Texture2D PlayerBody { get; private set; } = null!;
    public static Texture2D PlayerLeftArm { get; private set; } = null!;
    public static Texture2D PlayerRightArm { get; private set; } = null!;
    public static Texture2D PlayerLeg { get; private set; } = null!;
    
    #endregion

    private MouseState _previousMouseState;
    private MouseState _currentMouseState;
    
    public SpriteFont MainFont { get; private set; }
    private Texture2D _logoTexture;
    
    #region Menus
    
    public static LogoMenu? LogoMenu { get; private set; }
    public static MainMenu? MainMenu { get; private set; }
    public static SinglePlayerMenu? SinglePlayerMenu { get; private set; }
    public static PlayerCreationMenu? PlayerCreationMenu { get; private set; }
    public static SettingsMenu? SettingsMenu { get; private set; }
    public static AudioSettingsMenu? AudioSettingsMenu { get; private set; }
    public static VideoSettingsMenu? VideoSettingsMenu { get; private set; }
    private readonly List<Menu> _menus;
    
    #endregion

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

    private static readonly Random Rand = new();

    private double _colorTimer;
    public static Color ErrorColor { get; private set; }
    
    public BlasterMasterGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _menus = new List<Menu>();
        
        // root folder of all assets
        Content.RootDirectory = "Main/Content";
        SoundEngine.Initialize(Content);
        MusicEngine.Initialize(Content);
        
        // initialize video manager with graphics manager
        string videoManagerSavePath = Content.RootDirectory + Paths.VideoManagerSavePath;
        VideoManager.Instance.Initialize(_graphics, videoManagerSavePath);
        VideoManager.Instance.LoadStateFromFile();
        // load audio manager
        string audioManagerSavePath = Content.RootDirectory + Paths.AudioManagerSavePath;
        AudioManager.Instance.Initialize(audioManagerSavePath);
        AudioManager.Instance.LoadStateFromFile();
        // load player manager
        string playerManagerSavePath = Content.RootDirectory + Paths.PlayerSavePath;
        PlayerManager.Instance.Initialize(playerManagerSavePath);
        
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

    private void LoadTextures()
    {
        CursorTexture = LoadingUtilities.LoadTexture(GraphicsDevice, 
            Content.RootDirectory + Paths.CursorTexturePath);
        
        _logoTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.Logo5XTexturePath);
        
        SliderTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.SliderBackgroundPath);
        
        WhitePixel = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.WhitePixelPath);
        
        InvisibleTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.InvisibleTexturePath);
        
        #region Player Textures
        
        PlayerHead = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.PlayerHeadTexturePath);
        
        PlayerBody = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.PlayerBodyTexturePath);
        
        PlayerLeftArm = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.PlayerLeftArmTexturePath);
        
        PlayerRightArm = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.PlayerRightArmTexturePath);
        
        PlayerLeg = LoadingUtilities.LoadTexture(GraphicsDevice,
            Content.RootDirectory + Paths.PlayerLegTexturePath);
        
        #endregion
    }
    
    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        
        SoundEngine.LoadSounds();
        MusicEngine.LoadMusic();
        MusicEngine.PlayMusicTrack(ChooseRandomMenuMusic());
        
        LoadTextures();
        MainFont = Content.Load<SpriteFont>("Font/Andy_24_Regular");
        
        
        LogoMenu = new LogoMenu(MainFont, _logoTexture);
        AddMenu(LogoMenu);
        
        MainMenu = new MainMenu(MainFont);
        AddMenu(MainMenu);
        
        SinglePlayerMenu = new SinglePlayerMenu(MainFont);
        AddMenu(SinglePlayerMenu);
        
        PlayerCreationMenu = new PlayerCreationMenu(MainFont);
        AddMenu(PlayerCreationMenu);
        
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
        MusicEngine.UnloadMusic();
    }

    #region Update
    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UpdateGameTime(gameTime);
        
        UpdateColors();
        
        UpdateMouseState();
        UpdateKeyboardState();

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
        
        // update previous states in the end
        UpdatePreviousStates();
    }

    private void UpdateColors()
    {
        _colorTimer += GameTimeElapsedSeconds;
        
        ErrorColor = MathUtilities.PingPongLerpColor(Color.Red, Color.DarkRed, 
            (float) _colorTimer, 0.4f);
    }

    private void UpdateGameTime(GameTime newGameTime)
    {
        GameTime = newGameTime;
        GameTimeElapsedSeconds = newGameTime.ElapsedGameTime.TotalSeconds;
    }

    private void UpdateMouseState()
    {
        _currentMouseState = Mouse.GetState();
        
        HasClickedLeft = _currentMouseState.LeftButton == ButtonState.Released 
                         && _previousMouseState.LeftButton == ButtonState.Pressed;

        IsHoldingLeft = _currentMouseState.LeftButton == ButtonState.Pressed;
        
        // subtract previous from current
        ScrollWheelDelta = _currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        
        // mouse position that is aligned with OS cursor
        CursorPosition = Vector2.Transform(new Vector2(_currentMouseState.X, _currentMouseState.Y),
            Matrix.Invert(VideoManager.Instance.CalculateResolutionScaleMatrix()));
    }

    private void UpdateKeyboardState()
    {
        KeyboardState = Keyboard.GetState();
    }

    private void UpdatePreviousStates()
    {
        _previousMouseState = _currentMouseState;
        PreviousKeyboardState = KeyboardState;
    }
    
    #endregion

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        var matrix = VideoManager.Instance.CalculateResolutionScaleMatrix();
        
        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, 
            null, matrix);
        
        foreach (Menu menu in _menus)
        {
            if (menu.Active)
            {
                menu.Draw(SpriteBatch);
            }
        }
        
        // draw cursor texture last on top of everything
        SpriteBatch.Draw(CursorTexture, CursorPosition, Color.White);
        SpriteBatch.End();
    }

    public static MusicID ChooseRandomMenuMusic()
    {
        float randomIndex = Rand.Next(0, 3);
        
        MusicID musicId = (MusicID)randomIndex;

        return musicId;
    }

    /// <summary>
    /// Requests to update the screen resolution by invoking the ResolutionRequestEvent.
    /// </summary>
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

    /// <summary>
    /// Requests to exit the game by invoking ExitRequestEvent
    /// </summary>
    public static void RequestExit()
    {
        ExitRequestEvent?.Invoke();
    }
}