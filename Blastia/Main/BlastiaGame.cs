using Blastia.Main.Commands;
using Blastia.Main.Entities;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Sounds;
using Blastia.Main.UI;
using Blastia.Main.UI.Menus;
using Blastia.Main.UI.Menus.InGame;
using Blastia.Main.UI.Menus.Settings;
using Blastia.Main.UI.Menus.SinglePlayer;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main;

public class BlastiaGame : Game
{
	/// <summary>
	/// Graphics manages the graphics device (connection between game and graphics hardware).
	/// Manages things such as resolution, VSync, fullscreen or windowed mode, etc.
	/// Used for low-level graphics operations.
	/// </summary>
	private readonly GraphicsDeviceManager _graphics;
	public static SpriteBatch SpriteBatch { get; private set; } = null!;
	private readonly SamplerState _pixelatedSamplerState;
	private const string CrashLogFileName = "crash_log.txt";
	private readonly string? _crashLogPath;
	
	// TICK
	public static GameTime GameTime { get; private set; } = new();
	/// <summary>
	/// Delta time between frames
	/// </summary>
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
	public static Texture2D RulerBlockHighlight { get; private set; } = null!;	
	public static Texture2D ProgressBarBackground { get; private set; } = null!;
	public static Texture2D WhitePixel { get; private set; } = null!;
	public static Texture2D InvisibleTexture { get; private set; } = null!;
	public static Texture2D MonitorTexture { get; private set; } = null!;
	public static Texture2D AudioTexture { get; private set; } = null!;
	public static Texture2D RedCrossTexture { get; private set; } = null!;
	public static Texture2D ExitTexture { get; private set; } = null!;

	private MouseState _previousMouseState;
	private MouseState _currentMouseState;
	
	public SpriteFont? MainFont { get; private set; }
	
	
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
	public static RulerMenu? RulerMenu { get; private set; }
	public static InGameMenu? InGameMenu { get; private set; }
	public static InGameSettingsMenu? InGameSettingsMenu { get; private set; }
	public static InGameVideoSettingsMenu? InGameVideoSettingsMenu { get; private set; }
	public static InGameAudioSettingsMenu? InGameAudioSettingsMenu { get; private set; }
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
	private static event Action? RequestWorldUnloadEvent;
	/// <summary>
	/// Event to request creation of DebugPoint entity for one frame
	/// </summary>
	private static event Action<Vector2, float>? RequestDebugPointDrawEvent;

	// RANDOM
	public static readonly Random Rand = new();

	// COLORS
	private double _colorTimer;
	public static Color ErrorColor { get; private set; }
	
	// CONSOLE
	public ConsoleWindow? ConsoleWindow;
	
	// GAMESTATE
	public World? World { get; private set; }
	private readonly List<Entity> _entities;
	private readonly List<Entity> _entitiesToRemove;
	private const ushort EntityLimit = 256;

	private Player? _myPlayer;
	private readonly List<Player> Players;
	public ushort PlayerLimit = 128;
	private bool IsWorldInitialized { get; set; }
	
	public BlastiaGame()
	{
		_crashLogPath = Path.Combine(Paths.GetSaveGameDirectory(), CrashLogFileName);
		if (!File.Exists(_crashLogPath)) File.Create(_crashLogPath).Close(); // create if doesnt exist
		File.WriteAllText(_crashLogPath, string.Empty); // remove everything
		
		InitializeConsole();
		
		_graphics = new GraphicsDeviceManager(this);
		_menus = new List<Menu>();
		_entities = new List<Entity>();
		_entitiesToRemove = new List<Entity>();
		Players = new List<Player>();
		
		// root folder of all assets
		Content.RootDirectory = "Main/Content";
		Paths.ContentRoot = Content.RootDirectory;
		SoundEngine.Initialize(Content);
		MusicEngine.Initialize(Content);
		
		// initialize video manager with graphics manager
		VideoManager.Instance.Initialize(_graphics);
		VideoManager.Instance.LoadStateFromFile<VideoManagerState>();
		// load audio manager
		AudioManager.Instance.Initialize();
		AudioManager.Instance.LoadStateFromFile<AudioManagerState>();
		// load player manager
		PlayerManager.Instance.Initialize();
		Console.WriteLine($"Save game directory: {Paths.GetSaveGameDirectory()}");
		
		ExitRequestEvent += OnExitRequested;
		ResolutionRequestEvent += UpdateResolution;
		RequestWorldInitializationEvent += InitializeWorld;
		RequestWorldUnloadEvent += UnloadWorld;
		RequestDebugPointDrawEvent += DrawDebugPoint;
		
		_pixelatedSamplerState = new SamplerState
		{
			Filter = TextureFilter.Point,
			AddressU = TextureAddressMode.Clamp,
			AddressV = TextureAddressMode.Clamp
		};
	}

	private void InitializeConsole()
	{
		ConsoleWindow = new ConsoleWindow();
		ConsoleWindow.Open("Blastia Game Console");
	}

	protected override void Initialize()
	{
		try
		{
			base.Initialize();

			int width = VideoManager.Instance.ResolutionHandler.GetCurrentResolutionWidth();
			int height = VideoManager.Instance.ResolutionHandler.GetCurrentResolutionHeight();
			_graphics.PreferredBackBufferWidth = width;
			_graphics.PreferredBackBufferHeight = height;
			_graphics.ApplyChanges();

			UpdateResolution();
		}
		catch (Exception ex)
		{
			LogError(ex, "Error in Initialize");
		}
	}

	private void LoadTextures()
	{		
		CursorTexture = LoadingUtilities.LoadTexture(GraphicsDevice, 
			Paths.CursorTexturePath);
		
		LogoTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.Logo5XTexturePath);
		
		SliderTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.SliderBackgroundPath);
		
		RulerBlockHighlight = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.RulerBlockHighlightPath);
			
		ProgressBarBackground = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.ProgressbarBackgroundPath);
		
		WhitePixel = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.WhitePixelPath);
		
		InvisibleTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.InvisibleTexturePath);
		
		MonitorTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.MonitorTexturePath);
		
		AudioTexture = LoadingUtilities.LoadTexture(GraphicsDevice,
			Paths.AudioTexturePath);

		RedCrossTexture = LoadingUtilities.LoadTexture(GraphicsDevice, Paths.RedCrossPath);
		ExitTexture = LoadingUtilities.LoadTexture(GraphicsDevice, Paths.ExitPath);
	}
	
	protected override void LoadContent()
	{
		try
		{
			SpriteBatch = new SpriteBatch(GraphicsDevice);

			LoadTextures();

			SoundEngine.LoadSounds();
			MusicEngine.LoadMusic();
			MusicEngine.PlayMusicTrack(ChooseRandomMenuMusic());
			StuffLoader.LoadBlocks(GraphicsDevice);
			StuffLoader.LoadHumans(GraphicsDevice);

			MainFont = Content.Load<SpriteFont>("Font/Raleway_24_Regular");

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
			
			RulerMenu = new RulerMenu(MainFont);
			AddMenu(RulerMenu);

			InGameMenu = new InGameMenu(MainFont);
			AddMenu(InGameMenu);
			
			InGameSettingsMenu = new InGameSettingsMenu(MainFont);
			AddMenu(InGameSettingsMenu);
			
			InGameVideoSettingsMenu = new InGameVideoSettingsMenu(MainFont);
			AddMenu(InGameVideoSettingsMenu);
			
			InGameAudioSettingsMenu = new InGameAudioSettingsMenu(MainFont);
			AddMenu(InGameAudioSettingsMenu);
		}
		catch (Exception ex)
		{
			LogError(ex, "Error in LoadContent");
		}
	}

	protected override void UnloadContent()
	{
		UnloadWorld();
		ConsoleWindow?.Close();
		
		base.UnloadContent();
		
		SoundEngine.UnloadSounds();
		MusicEngine.UnloadMusic();
	}

	// UPDATE
	protected override void Update(GameTime gameTime)
	{
		try
		{
			base.Update(gameTime);
			UpdateGameTime(gameTime);

			UpdateColors();

			UpdateMouseState();
			UpdateKeyboardState();

			if (IsWorldInitialized)
			{
				if (World != null)
				{
					if (_myPlayer?.Camera != null)
					{
						var pos = _myPlayer.Camera.ScreenToWorld(CursorPosition);
						if (KeyboardState.IsKeyDown(Keys.E)) World.SetRulerStart(pos);
						if (KeyboardState.IsKeyDown(Keys.F)) World.SetRulerEnd(pos);
						if (KeyboardHelper.IsKeyJustPressed(Keys.G)) World.DrawRulerLine();
					}
				
					World.Update();
				}
				
				_myPlayer?.Update();
				foreach (var player in Players)
				{
					player.Update();
				}

				var entities = _entities.ToList();
				foreach (var entity in entities)
				{
					entity.Update();
				}
			}

			foreach (Menu menu in _menus)
			{
				if (menu.Active)
				{
					if (menu.CameraUpdate && _myPlayer?.Camera != null) menu.Update(_myPlayer.Camera);
					else menu.Update();
					
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
		catch (Exception ex)
		{
			LogError(ex, "Error in Update");
		}
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

	// DRAW
	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.CornflowerBlue);
		
		var matrix = VideoManager.Instance.CalculateResolutionScaleMatrix();
		
		SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
			_pixelatedSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, 
			null, matrix);
		
		if (IsWorldInitialized && PlayerManager.Instance.SelectedWorld != null)
		{
			_myPlayer?.Camera?.RenderWorld(SpriteBatch, PlayerManager.Instance.SelectedWorld);
			_myPlayer?.Camera?.RenderEntity(SpriteBatch, _myPlayer);

			foreach (var entity in _entities)
			{
				_myPlayer?.Camera?.RenderEntity(SpriteBatch, entity);
			}
			
			// after updating and drawing each entity one time, remove ones that are scheduled
			foreach (var entityToRemove in _entitiesToRemove)
			{
				_entities.Remove(entityToRemove);
			}
			_entitiesToRemove.Clear();
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
		float randomIndex = Rand.Next(0, 4);
		
		MusicID musicId = (MusicID)randomIndex;

		return musicId;
	}

	// UPDATE RESOLUTION
	/// <summary>
	/// Requests to update the screen resolution by invoking the ResolutionRequestEvent.
	/// </summary>
	public static void RequestResolutionUpdate() => ResolutionRequestEvent?.Invoke();
	private void UpdateResolution()
	{
		ScreenWidth = GraphicsDevice.Viewport.Width;
		ScreenHeight = GraphicsDevice.Viewport.Height;
	}
	
	// WORLD INITIALIZATION
	public static void RequestWorldInitialization() => RequestWorldInitializationEvent?.Invoke();
	private void InitializeWorld()
	{
		var worldState = PlayerManager.Instance.SelectedWorld;
		if (worldState == null) return;
		
		World = new World(worldState);
		_myPlayer = new Player(worldState.GetSpawnPoint(), 0.2f, true);
		World.SetPlayer(_myPlayer);
		
		if (LogoMenu != null) LogoMenu.Active = false;
		if (WorldsMenu != null) WorldsMenu.Active = false;
		if (RulerMenu != null) RulerMenu.Active = World.RulerMode;
		if (InGameMenu != null) InGameMenu.Active = true;
		
		ConsoleWindow?.InitializeWorldCommands(World);
		
		_entities.Add(new MutantScavenger(new Vector2(50, 50)));

		IsWorldInitialized = true;
		
		// subscribe each UI element with interface to OnZoomed Camera method
		if (_myPlayer.Camera != null)
		{
			foreach (var menu in _menus)
			{
				foreach (var element in menu.Elements)
				{
					if (element is IWorldPositionUi cameraScalable)
					{
						_myPlayer.Camera.OnPositionChanged += cameraScalable.OnChangedPosition;
						_myPlayer.Camera.OnZoomed += cameraScalable.OnChangedZoom;
					}
				}
			}
		}
	}

	public static void RequestWorldUnload() => RequestWorldUnloadEvent?.Invoke();
	private void UnloadWorld()
	{
		if (LogoMenu != null) LogoMenu.Active = true;
		
		ConsoleWindow?.UnloadWorldCommands();
		
		IsWorldInitialized = false;
		World?.Unload();
		World = null;
		_myPlayer = null;
		_entities.Clear();
	}

	// DRAW DEBUG POINT
	/// <summary>
	/// Creates DebugPoint entity and draws if for one frame
	/// </summary>
	/// <param name="position">Position of DebugPoint</param>
	/// <param name="scale">Scale of DebugPoint</param>
	public static void RequestDebugPointDraw(Vector2 position, float scale = 1f) => 
		RequestDebugPointDrawEvent?.Invoke(position, scale);
	private void DrawDebugPoint(Vector2 position, float scale = 1f)
	{
		// draw debug point for this frame
		var debugPoint = new DebugPoint(position, scale);
		if (_entities.Count <= EntityLimit)
		{
			_entities.Add(debugPoint);
		}
		
		// schedule removal for next frame
		_entitiesToRemove.Add(debugPoint);
	}
	
	// EXIT
	/// <summary>
	/// Requests to exit the game by invoking ExitRequestEvent
	/// </summary>
	public static void RequestExit() => ExitRequestEvent?.Invoke();
	private void OnExitRequested()
	{
		ConsoleWindow?.Close();
		
		Exit();
	}

	private void AddMenu(Menu? menu)
	{
		if (menu != null) _menus.Add(menu);
	}

	/// <summary>
	/// Writes an error message to crash log (_crashLogPath)
	/// </summary>
	/// <param name="ex">Exception, writes its message, stacktrace and source</param>
	/// <param name="context">Additional information about the error</param>
	private void LogError(Exception ex, string context = "")
	{
		if (!string.IsNullOrEmpty(_crashLogPath))
		{
			string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			string errorMessage = $"[{time}] {context} \n" +
			                      $"Message: {ex.Message} \n" +
			                      $"Stack Trace: {ex.StackTrace} \n" +
			                      $"Source: {ex.Source} \n";
			
			File.AppendAllText(_crashLogPath, errorMessage);
			Console.WriteLine($"Error logged: {context}");
		}
		else
		{
			Console.WriteLine("Crash Log File not initialized");
		}
	}
}
