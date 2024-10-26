using BlasterMaster.Main.Sounds;
using BlasterMaster.Main.Blocks.Common;
using BlasterMaster.Main.UI;
using BlasterMaster.Main.UI.Menus;
using BlasterMaster.Main.UI.Menus.Settings;
using BlasterMaster.Main.UI.Menus.SinglePlayer;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BlasterMaster.Main.GameState;

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
	public static Texture2D LogoTexture { get; private set; } = null!;
	public static Texture2D CursorTexture { get; private set; } = null!;
	public static Texture2D SliderTexture { get; private set; } = null!;	
	public static Texture2D ProgressBarBackground { get; private set; } = null!;
	public static Texture2D WhitePixel { get; private set; } = null!;
	public static Texture2D InvisibleTexture { get; private set; } = null!;
	
	public class PlayerTextures 
	{
		public static Texture2D PlayerHead { get; set; } = null!;
		public static Texture2D PlayerBody { get; set; } = null!;
		public static Texture2D PlayerLeftArm { get; set; } = null!;
		public static Texture2D PlayerRightArm { get; set; } = null!;
		public static Texture2D PlayerLeg { get; set; } = null!;
	}

	private MouseState _previousMouseState;
	private MouseState _currentMouseState;
	
	public SpriteFont MainFont { get; private set; }
	
	
	// MENUS
	public static LogoMenu? LogoMenu { get; private set; }
	public static MainMenu? MainMenu { get; private set; }
	public static PlayersMenu? PlayersMenu { get; private set; }
	public static WorldsMenu? WorldsMenu { get; private set; }
	public static PlayerCreationMenu? PlayerCreationMenu { get; private set; }	
	public static WorldCreationMenu? WorldCreationMenu { get; private set; }
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
	
	/// <summary>
	/// Event to request world initialization 
	/// </summary>
	private static event Action? RequestWorldInitializationEvent;

	private static readonly Random Rand = new();

	// COLORS
	private double _colorTimer;
	public static Color ErrorColor { get; private set; }
	
	// GAMESTATE
	private World _currentWorld;
	private Camera _gameCamera;
	
	public BlasterMasterGame()
	{
		_graphics = new GraphicsDeviceManager(this);
		_menus = new List<Menu>();
		
		// root folder of all assets
		Content.RootDirectory = "Main/Content";
		Paths.ContentRoot = Content.RootDirectory;
		SoundEngine.Initialize(Content);
		MusicEngine.Initialize(Content);
		
		// initialize video manager with graphics manager
		string videoManagerSavePath = Paths.VideoManagerSavePath;
		VideoManager.Instance.Initialize(_graphics, videoManagerSavePath);
		VideoManager.Instance.LoadStateFromFile();
		// load audio manager
		string audioManagerSavePath = Paths.AudioManagerSavePath;
		AudioManager.Instance.Initialize(audioManagerSavePath);
		AudioManager.Instance.LoadStateFromFile();
		// load player manager
		string playersSavePath = Paths.PlayerSavePath;
		string worldsSavePath = Paths.WorldsSavePath;
		PlayerManager.Instance.Initialize(playersSavePath, worldsSavePath);
		
		ExitRequestEvent += OnExitRequested;
		ResolutionRequestEvent += UpdateResolution;
		RequestWorldInitializationEvent += InitializeWorld;
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
			Paths.CursorTexturePath);
		
		LogoTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.Logo5XTexturePath);
		
		SliderTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.SliderBackgroundPath);
			
		ProgressBarBackground = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.ProgrssbarBackgroundPath);
		
		WhitePixel = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.WhitePixelPath);
		
		InvisibleTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.InvisibleTexturePath);
			
		PlayerTextures.PlayerHead = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.PlayerHeadTexturePath);
		
		PlayerTextures.PlayerBody = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.PlayerBodyTexturePath);
		
		PlayerTextures.PlayerLeftArm = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.PlayerLeftArmTexturePath);
		
		PlayerTextures.PlayerRightArm = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.PlayerRightArmTexturePath);
		
		PlayerTextures.PlayerLeg = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.PlayerLegTexturePath);
	}
	
	protected override void LoadContent()
	{
		SpriteBatch = new SpriteBatch(GraphicsDevice);
		
		SoundEngine.LoadSounds();
		MusicEngine.LoadMusic();
		MusicEngine.PlayMusicTrack(ChooseRandomMenuMusic());
		BlockLoader.LoadBlocks(GraphicsDevice);
		
		LoadTextures();
		MainFont = Content.Load<SpriteFont>("Font/Andy_24_Regular");
		
		// menus
		LogoMenu = new LogoMenu(MainFont);
		AddMenu(LogoMenu);
		
		MainMenu = new MainMenu(MainFont);
		AddMenu(MainMenu);
		
		PlayersMenu = new PlayersMenu(MainFont);
		AddMenu(PlayersMenu);
		
		WorldsMenu = new WorldsMenu(MainFont);
		AddMenu(WorldsMenu);
		
		PlayerCreationMenu = new PlayerCreationMenu(MainFont);
		AddMenu(PlayerCreationMenu);
		
		WorldCreationMenu = new WorldCreationMenu(MainFont);
		AddMenu(WorldCreationMenu);
		
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
			
		if (_currentWorld != null) 
		{
			_currentWorld.Draw(SpriteBatch);
		}
		
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
	
	public static void RequestWorldInitialization() 
	{
		RequestWorldInitializationEvent?.Invoke();
	}	
	
	private void InitializeWorld() 
	{
		// load the world if it is selected
		if (PlayerManager.Instance.SelectedWorld == null) return;
		
		_gameCamera = new Camera(Vector2.Zero) 
		{
			DrawWidth = 100,
			DrawHeight = 100
		};
		
		_currentWorld = new World(PlayerManager.Instance.SelectedWorld, _gameCamera);
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
	
	private void AddMenu(Menu? menu)
	{
		if (menu != null) _menus.Add(menu);
	}
}