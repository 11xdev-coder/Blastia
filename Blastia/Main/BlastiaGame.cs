﻿using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Sounds;
using Blastia.Main.UI;
using Blastia.Main.UI.Menus;
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
	private SamplerState _pixelatedSamplerState;
	
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
	private Player _player;
	public bool IsWorldInitialized { get; private set; }
	
	public BlastiaGame()
	{
		_graphics = new GraphicsDeviceManager(this);
		_menus = new List<Menu>();
		
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
		
		_pixelatedSamplerState = new SamplerState
		{
			Filter = TextureFilter.Point,
			AddressU = TextureAddressMode.Clamp,
			AddressV = TextureAddressMode.Clamp
		};
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
	}
	
	protected override void LoadContent()
	{
		SpriteBatch = new SpriteBatch(GraphicsDevice);
		
		SoundEngine.LoadSounds();
		MusicEngine.LoadMusic();
		MusicEngine.PlayMusicTrack(ChooseRandomMenuMusic());
		StuffLoader.LoadBlocks(GraphicsDevice);
		StuffLoader.LoadHumans(GraphicsDevice);
		
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

		if (IsWorldInitialized)
		{
			_player.Update();
		}

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
			_pixelatedSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, 
			null, matrix);
		
		if (IsWorldInitialized && PlayerManager.Instance.SelectedWorld != null)
		{
			_player.Camera.RenderWorld(SpriteBatch, PlayerManager.Instance.SelectedWorld);
			_player.Camera.RenderPlayer(SpriteBatch, _player); // TODO: list of players for multiplayer
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

	// UPDATE RESOLUTION
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
	
	// WORLD INITIALIZATION
	public static void RequestWorldInitialization() 
	{
		RequestWorldInitializationEvent?.Invoke();
	}	
	
	private void InitializeWorld()
	{
		_player = new Player(Vector2.Zero, 0.2f);

		IsWorldInitialized = true;
	}
	
	// EXIT
	/// <summary>
	/// Requests to exit the game by invoking ExitRequestEvent
	/// </summary>
	public static void RequestExit()
	{
		ExitRequestEvent?.Invoke();
	}

	private void OnExitRequested()
	{
		Exit();
	}
		
	private void AddMenu(Menu? menu)
	{
		if (menu != null) _menus.Add(menu);
	}
}