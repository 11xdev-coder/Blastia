using Blastia.Main.Commands;
using Blastia.Main.Entities;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Networking;
using Blastia.Main.Sounds;
using Blastia.Main.UI;
using Blastia.Main.UI.Menus;
using Blastia.Main.UI.Menus.InGame;
using Blastia.Main.UI.Menus.Multiplayer;
using Blastia.Main.UI.Menus.Settings;
using Blastia.Main.UI.Menus.SinglePlayer;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO; // for Path and File
using Blastia.Main.Blocks.Common;
using Blastia.Main.Physics;
using Blastia.Main.Blocks; // for Block.Size

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
	public static bool HasClickedRight { get; private set; }
	public static Vector2 CursorPosition { get; private set; }
	public static bool IsHoldingLeft { get; private set; }
	public static float ScrollWheelDelta { get; private set; }
	public static bool IsHoveredOnAnyUi { get; set; }
	
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
	public static Texture2D SlotBackgroundTexture { get; private set; } = null!;
	public static Texture2D SlotHighlightedTexture { get; private set; } = null!;
	public static Texture2D BlockDestroyTexture { get; private set; } = null!;
	public static Texture2D SignEditBackgroundTexture { get; private set; } = null!;
	public static Texture2D SignWrittenOverlay1Texture { get; private set; } = null!;
	public static Texture2D SignWrittenOverlay2Texture { get; private set; } = null!;

	private MouseState _previousMouseState;
	private MouseState _currentMouseState;
	
	public static SpriteFont? MainFont { get; private set; }
	
	// MENUS
	public static LogoMenu? LogoMenu { get; private set; }
	public static MainMenu? MainMenu { get; private set; }
	public static PlayersMenu? PlayersMenu { get; private set; }
	public static WorldsMenu? WorldsMenu { get; private set; }
	public static PlayerCreationMenu? PlayerCreationMenu { get; private set; }	
	public static WorldCreationMenu? WorldCreationMenu { get; private set; }
	public static MultiplayerMenu? MultiplayerMenu { get; private set; }
	public static JoinGameMenu? JoinGameMenu { get; private set; }
	public static SettingsMenu? SettingsMenu { get; private set; }
	public static AudioSettingsMenu? AudioSettingsMenu { get; private set; }
	public static VideoSettingsMenu? VideoSettingsMenu { get; private set; }
	public static RulerMenu? RulerMenu { get; private set; }
	public static InGameMenu? InGameMenu { get; private set; }
	public static FpsCounterMenu? FpsCounterMenu { get; private set; }
	public static InGameSettingsMenu? InGameSettingsMenu { get; private set; }
	public static InGameVideoSettingsMenu? InGameVideoSettingsMenu { get; private set; }
	public static InGameAudioSettingsMenu? InGameAudioSettingsMenu { get; private set; }
	public static InGameSignEditMenu? InGameSignEditMenu { get; private set; }
	public static InventoryUi? PlayerInventoryUiMenu { get; private set; }
	public static PlayerStatsMenu? PlayerStatsMenu { get; private set; }
	private readonly List<Menu> _menus = [];
	private readonly List<Menu> _menusToAdd = [];

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
	private static event Action<Entity>? RequestAddEntityEvent;
	private static event Action<Entity>? RequestRemoveEntityEvent;
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
	/// <summary>
	/// Use <c>World</c> to get public readonly list of entities
	/// </summary>
	private readonly List<Entity> _entities;
	private readonly List<Entity> _entitiesToRemove;
	private const ushort EntityLimit = 256;

	private Player? _myPlayer;
	private readonly List<Player> _players;
	public const float PlayerScale = 0.15f;
	public ushort PlayerLimit = 128;
	private bool IsWorldInitialized { get; set; }
	
	/// <summary>
	/// Used for showing tooltips under the cursor
	/// </summary>
	public static TooltipDisplay? TooltipDisplay { get; private set; }
	public static NotificationDisplay? NotificationDisplay { get; private set; }
	
	public BlastiaGame()
	{
		_crashLogPath = Path.Combine(Paths.GetSaveGameDirectory(), CrashLogFileName);
		if (!File.Exists(_crashLogPath)) File.Create(_crashLogPath).Close(); // create if doesnt exist
		File.WriteAllText(_crashLogPath, string.Empty); // remove everything
		
		InitializeConsole();
		RedirectConsoleOutput();
		
		_graphics = new GraphicsDeviceManager(this);
		UncapFps();
		
		_entities = new List<Entity>();
		_entitiesToRemove = new List<Entity>();
		_players = new List<Player>();
		
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
		PlayerNWorldManager.Instance.Initialize();
		Console.WriteLine($"Save game directory: {Paths.GetSaveGameDirectory()}");
		
		ExitRequestEvent += OnExitRequested;
		ResolutionRequestEvent += UpdateResolution;
		RequestWorldInitializationEvent += InitializeWorld;
		RequestWorldUnloadEvent += UnloadWorld;
		RequestAddEntityEvent += AddEntity;
		RequestRemoveEntityEvent += RemoveEntity;
		RequestDebugPointDrawEvent += DrawDebugPoint;
		
		_pixelatedSamplerState = new SamplerState
		{
			Filter = TextureFilter.Point,
			AddressU = TextureAddressMode.Clamp,
			AddressV = TextureAddressMode.Clamp
		};

		NetworkManager.Instance = new NetworkManager();
		if (NetworkManager.Instance.InitializeSteam())
		{
			// steam initialized
			NetworkEntitySync.Initialize(() => _myPlayer, () => _players, _players.Add, () => _entities, AddEntity, () => World);
			NetworkBlockSync.Initialize(() => _players, () => World, () => _myPlayer);
		}
	}

	private void InitializeConsole()
	{
		try
		{
			ConsoleWindow = new ConsoleWindow();
			ConsoleWindow.Open("Blastia Game Console");
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"[BlastiaGame] Console initialization failed: {ex.Message}");
			ConsoleWindow = null;
		}
	}

	private void RedirectConsoleOutput()
	{
		Console.SetOut(new FilteredTextWriter(Console.Out, line => 
			!line.Contains("KEY/SCANCODE MISSING FROM SDL2->XNA DICTIONARY:")));
	}

	private void UncapFps()
	{
		// uncap framerate for accurate FPS measurement
		IsFixedTimeStep = false;
		_graphics.SynchronizeWithVerticalRetrace = false;
		_graphics.ApplyChanges();
		GraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
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
		CursorTexture = Util.LoadTexture(GraphicsDevice, 
			Paths.CursorTexturePath);
		
		LogoTexture = Util.LoadTexture(GraphicsDevice,
			Paths.Logo5XTexturePath);
		
		SliderTexture = Util.LoadTexture(GraphicsDevice,
			Paths.SliderBackgroundPath);
		
		RulerBlockHighlight = Util.LoadTexture(GraphicsDevice,
			Paths.RulerBlockHighlightPath);
			
		ProgressBarBackground = Util.LoadTexture(GraphicsDevice,
			Paths.ProgressbarBackgroundPath);
		
		WhitePixel = Util.LoadTexture(GraphicsDevice,
			Paths.WhitePixelPath);
		
		InvisibleTexture = Util.LoadTexture(GraphicsDevice,
			Paths.InvisibleTexturePath);
		
		MonitorTexture = Util.LoadTexture(GraphicsDevice,
			Paths.MonitorTexturePath);
		
		AudioTexture = Util.LoadTexture(GraphicsDevice,
			Paths.AudioTexturePath);

		RedCrossTexture = Util.LoadTexture(GraphicsDevice, Paths.RedCrossPath);
		ExitTexture = Util.LoadTexture(GraphicsDevice, Paths.ExitPath);
		SlotBackgroundTexture = Util.LoadTexture(GraphicsDevice, Paths.SlotBackgroundTexturePath);
		SlotHighlightedTexture = Util.LoadTexture(GraphicsDevice, Paths.SlotHighlightedTexturePath);
		BlockDestroyTexture = Util.LoadTexture(GraphicsDevice, Paths.BlockDestroyTexturePath);
		SignEditBackgroundTexture = Util.LoadTexture(GraphicsDevice, Paths.SignEditBackgroundTexturePath);
		SignWrittenOverlay1Texture = Util.LoadTexture(GraphicsDevice, Paths.SignWrittenOverlay1TexturePath);
		SignWrittenOverlay2Texture = Util.LoadTexture(GraphicsDevice, Paths.SignWrittenOverlay2TexturePath);
	}
	
	protected override void LoadContent()
	{
		try
		{
			Exiting += (_, _) => NetworkManager.Instance?.Shutdown();
			SpriteBatch = new SpriteBatch(GraphicsDevice);

			LoadTextures();

			SoundEngine.LoadSounds();
			MusicEngine.LoadMusic();
			MusicEngine.PlayMusicTrack(ChooseRandomMenuMusic());
			StuffLoader.LoadBlocks(GraphicsDevice);
			StuffLoader.LoadHumans(GraphicsDevice);
			StuffLoader.LoadItemsFromJson(GraphicsDevice, Paths.ItemsData);

			MainFont = Content.Load<SpriteFont>("Font/Raleway");

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
			
			MultiplayerMenu = new MultiplayerMenu(MainFont);
			AddMenu(MultiplayerMenu);
			
			JoinGameMenu = new JoinGameMenu(MainFont);
			AddMenu(JoinGameMenu);

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
			
			FpsCounterMenu = new FpsCounterMenu(MainFont);
			AddMenu(FpsCounterMenu);
			
			InGameSettingsMenu = new InGameSettingsMenu(MainFont);
			AddMenu(InGameSettingsMenu);
			
			InGameVideoSettingsMenu = new InGameVideoSettingsMenu(MainFont);
			AddMenu(InGameVideoSettingsMenu);
			
			InGameAudioSettingsMenu = new InGameAudioSettingsMenu(MainFont);
			AddMenu(InGameAudioSettingsMenu);
			
			InGameSignEditMenu = new InGameSignEditMenu(MainFont);
			AddMenu(InGameSignEditMenu);
			
			PlayerStatsMenu = new PlayerStatsMenu(MainFont);
			AddMenu(PlayerStatsMenu);
			
			TooltipDisplay = new TooltipDisplay(MainFont);
			NotificationDisplay = new NotificationDisplay(MainFont);
		}
		catch (Exception ex)
		{
			LogError(ex, "Error in LoadContent");
		}
	}

	protected override void UnloadContent()
	{
		NetworkManager.Instance?.Shutdown();
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
			SoundEngine.Update();

			if (NetworkManager.Instance != null && NetworkManager.Instance.IsSteamInitialized)
			{
				NetworkManager.Instance.Update();
			}

			UpdateColors();

			UpdateMouseState();
			UpdateKeyboardState();
			NotificationDisplay?.Update();
			TooltipDisplay?.BeginFrame();

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

			if (_menusToAdd.Count > 0)
			{
				_menus.AddRange(_menusToAdd);
				_menusToAdd.Clear();
			}
			
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

					if (PlayerNWorldManager.Instance.SelectedWorld != null)
					{
						// then add tiles and update tiles
						Collision.ClearGrid();
						
						// first add entities to grid
						if (_myPlayer != null)
						{
							var rect = _myPlayer.GetBounds();
							Collision.AddObjectToGrid(rect, false, _myPlayer);
						}
						
						if (NetworkManager.Instance != null && NetworkManager.Instance.IsHost) 
						{
							var tuple = _myPlayer?.Camera?.SetDrawnTiles(PlayerNWorldManager.Instance.SelectedWorld);
							var collisionBodies = tuple?.Item1;
							var tilesToUpdate = tuple?.Item2;
							
						    // loop through each remote player and get their drawn tiles
						    foreach (var p in _players) 
						    {
						        var remotePlayerTuple = p.Camera?.SetDrawnTiles(PlayerNWorldManager.Instance.SelectedWorld);
								if (remotePlayerTuple?.Item2 != null)
								{
									foreach (var kvp in remotePlayerTuple.GetValueOrDefault().Item2)
									{
										tilesToUpdate?.TryAdd(kvp.Key, kvp.Value);
									}
								}
						    }
						    
						    if (collisionBodies == null || tilesToUpdate == null) return;

							var blocksUpdatedThisFrame = new HashSet<Vector2>();
							foreach (var (t, blockInstance) in tilesToUpdate)
							{
								var position = t.Item1;
								try
								{
									var beforeUpdate = CaptureBlockState(blockInstance, position);
									blockInstance.Update(World, position);
									var afterUpdate = CaptureBlockState(blockInstance, position);

									var hasStateChanged = HasBlockStateChanged(beforeUpdate, afterUpdate);
									var isLiquidChanged = blockInstance.Block is LiquidBlock liquid && liquid.HasChangedThisFrame; 
									
									if (hasStateChanged || isLiquidChanged) // if state changed
										blocksUpdatedThisFrame.Add(position); // add to updated blocks
									
									if (blockInstance.Block.IsCollidable && collisionBodies.TryGetValue(position, out var box))
									{
										Collision.AddObjectToGrid(box, true);
									}
								}
								catch (Exception ex)
								{
									Console.WriteLine($"[HOST] Error updating block at {position}. Exception: {ex.Message}");
								}
							}
							
							if (blocksUpdatedThisFrame.Count > 0 && NetworkManager.Instance != null && NetworkManager.Instance.IsConnected) 
							{
								NetworkBlockSync.BroadcastUpdatedBlocksToClients(blocksUpdatedThisFrame);
							}
						}
						else if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
						{
						    // on client only set drawn tiles to render + handle collision
						    // updates are received from host
						    var tuple = _myPlayer?.Camera?.SetDrawnTiles(PlayerNWorldManager.Instance.SelectedWorld);
						    var collisionBodies = tuple?.Item1;
							var tilesToUpdate = tuple?.Item2;

							if (collisionBodies == null || tilesToUpdate == null) return;

							foreach (var (t, blockInstance) in tilesToUpdate)
							{
								var position = t.Item1;
								try
								{									
									if (blockInstance.Block.IsCollidable && collisionBodies.TryGetValue(position, out var box))
									{
										Collision.AddObjectToGrid(box, true);
									}
								}
								catch (Exception ex)
								{
									Console.WriteLine($"[CLIENT] Error handling block collision at {position}. Exception: {ex.Message}");
								}
							}
						}
					}

					World.Update();
				}

				// then update entities
				_myPlayer?.Update();
				foreach (var player in _players)
				{
					player.Update();
				}

				foreach (var entity in _entities)
				{
					entity.Update();
				}
			}

			TooltipDisplay?.SetPlayerCamera(_myPlayer?.Camera);
			TooltipDisplay?.Update();

			// update previous states in the end
			UpdatePreviousStates();
		}
		catch (Exception ex)
		{
			LogError(ex, "Error in Update");
		}
	}
	
	/// <summary>
	/// Returns tuple of <c>ID</c>, <c>Damage</c> and <c>FlowLevel</c> (0 if not liquid) properties of block instance
	/// </summary>
	/// <returns></returns>
	private (ushort blockId, float damage, int flowLevel) CaptureBlockState(BlockInstance inst, Vector2 position) 
	{
		var worldState = PlayerNWorldManager.Instance.SelectedWorld;
    
		// get the actual block ID from the world (this detects if block was created/destroyed during update)
		var actualBlockId = worldState?.GetTile((int)position.X, (int)position.Y, inst.Block.GetLayer()) ?? 0;
		
		var flowLevel = 0;
		if (inst.Block is LiquidBlock liquid)
			flowLevel = liquid.FlowLevel;

		return (actualBlockId, inst.Damage, flowLevel);
	}
	
	/// <summary>
	/// Checks if <c>before</c> tuple changed enough from <c>after</c> tuple
	/// </summary>
	/// <returns></returns>
	private bool HasBlockStateChanged((ushort blockId, float damage, int flowLevel) before, (ushort blockId, float damage, int flowLevel) after) 
	{
		if (before.blockId != after.blockId) return true;

		// enough damage
		if (Math.Abs(before.damage - after.damage) > 0.01f) return true;

		if (before.flowLevel != after.flowLevel) return true;

		return false;
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
		
		HasClickedRight = _currentMouseState.RightButton == ButtonState.Released 
		                 && _previousMouseState.RightButton == ButtonState.Pressed;

		IsHoldingLeft = _currentMouseState.LeftButton == ButtonState.Pressed;
		
		// subtract previous from current
		ScrollWheelDelta = _currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
		
		// mouse position that is aligned with OS cursor
		CursorPosition = Vector2.Transform(new Vector2(_currentMouseState.X, _currentMouseState.Y),
			Matrix.Invert(VideoManager.Instance.CalculateResolutionScaleMatrix()));

		// reset this flag, will be set in any UIElement that is hovered on
		IsHoveredOnAnyUi = false;
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

		if (IsWorldInitialized && PlayerNWorldManager.Instance.SelectedWorld != null)
		{
			// first ground
			_myPlayer?.Camera?.RenderGroundTiles(SpriteBatch, PlayerNWorldManager.Instance.SelectedWorld);
			if (World is {DrawCollisionGrid: true}) _myPlayer?.Camera?.RenderSpatialGrid(SpriteBatch, PlayerNWorldManager.Instance.SelectedWorld);
			
			// then entities
			_myPlayer?.Camera?.RenderEntity(SpriteBatch, _myPlayer);
			foreach (var player in _players)
			{
				_myPlayer?.Camera?.RenderEntity(SpriteBatch, player);
			}
			
			foreach (var entity in _entities)
			{
				_myPlayer?.Camera?.RenderEntity(SpriteBatch, entity);
			}
			
			// then liquids and furniture
			_myPlayer?.Camera?.RenderFurnitureThenLiquids(SpriteBatch, PlayerNWorldManager.Instance.SelectedWorld);
			
			// after updating and drawing each entity one time, remove ones that are scheduled
			foreach (var entityToRemove in _entitiesToRemove)
			{
				_entities.Remove(entityToRemove);
			}
			_entitiesToRemove.Clear();
		}
		
		NotificationDisplay?.Draw(SpriteBatch);
		// any menus on top of the notifications
		foreach (Menu menu in _menus)
		{
			if (menu.Active)
			{
				menu.Draw(SpriteBatch);
			}
		}
		
		// draw cursor texture last on top of everything
		SpriteBatch.Draw(CursorTexture, CursorPosition, Color.White);
		TooltipDisplay?.Draw(SpriteBatch);
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
		var worldState = PlayerNWorldManager.Instance.SelectedWorld;
		if (worldState == null) return;
		
		World = new World(worldState, _entities.AsReadOnly());
		//worldState.SetSpawnPoint(1600, 468);
		_myPlayer = new Player(worldState.GetSpawnPoint(), World, PlayerScale, true);
		World.SetPlayer(_myPlayer);
		
		if (LogoMenu != null) LogoMenu.Active = false;
		if (WorldsMenu != null) WorldsMenu.Active = false;
		if (RulerMenu != null) RulerMenu.Active = World.RulerMode;
		if (InGameMenu != null) InGameMenu.Active = true;
		if (PlayerStatsMenu != null) PlayerStatsMenu.Active = true;
		
		ConsoleWindow?.InitializeWorldCommands(World);
		
		//_entities.Add(new MutantScavenger(new Vector2(50, 50)));

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
		
		InitializePlayerInventory();
	}

	/// <summary>
	/// Called during world initialization to create UI for player inventory
	/// </summary>
	private void InitializePlayerInventory()
	{
		if (MainFont == null || _myPlayer == null || World == null) return;
		
		var gridStartPosition = new Vector2(15, 45);
		var slotSize = new Vector2(1.5f);
		var slotSpacing = new Vector2(5f, 5f);

		PlayerInventoryUiMenu = new InventoryUi(MainFont, _myPlayer.PlayerInventory, World, gridStartPosition, Player.InventoryRows, 
			Player.InventoryColumns, slotSize, slotSpacing, SlotBackgroundTexture, SlotHighlightedTexture, false, true);
		AddMenu(PlayerInventoryUiMenu);

		_myPlayer.PlayerInventory.AddItem(StuffRegistry.GetItem(ItemId.CandyBlock), 100);
		_myPlayer.PlayerInventory.AddItem(StuffRegistry.GetItem(ItemId.AppleCandyBlock), 100);
		_myPlayer.PlayerInventory.AddItem(StuffRegistry.GetItem(ItemId.BlueberryCandyBlock), 100);
		_myPlayer.PlayerInventory.AddItem(StuffRegistry.GetItem(ItemId.WaterBucket), 20);
		_myPlayer.PlayerInventory.AddItem(StuffRegistry.GetItem(ItemId.LavaBucket), 20);
		_myPlayer.PlayerInventory.AddItem(StuffRegistry.GetItem(ItemId.EmptyBucket), 20);
		_myPlayer.PlayerInventory.AddItem(StuffRegistry.GetItem(ItemId.SignBlock), 100);
	}
	
	// WORLD UNLOADING
	public static void RequestWorldUnload() => RequestWorldUnloadEvent?.Invoke();
	private void UnloadWorld()
	{
		// save world state before unloading
		var worldState = PlayerNWorldManager.Instance.SelectedWorld;
		if (worldState != null)
		{
			var savePath = Path.Combine(PlayerNWorldManager.Instance.WorldsSaveFolder, worldState.Name + ".bmwld");
			Saving.Save(savePath, worldState);
		}
		
		if (LogoMenu != null) LogoMenu.Active = true;
		if (PlayerStatsMenu != null) PlayerStatsMenu.Active = false;
		
		ConsoleWindow?.UnloadWorldCommands();
		
		// unselect
		PlayerNWorldManager.Instance.UnselectPlayer();
		PlayerNWorldManager.Instance.UnselectWorld();
		
		IsWorldInitialized = false;
		World?.Unload();
		World = null;

		if (PlayerInventoryUiMenu != null)
		{
			_menus.Remove(PlayerInventoryUiMenu);
			PlayerInventoryUiMenu = null;
		}
		_myPlayer = null;
		_entities.Clear();
	}

	// ADD ENTITY
	public static void RequestAddEntity(Entity entity) => RequestAddEntityEvent?.Invoke(entity);
	private void AddEntity(Entity entity)
	{
		_entities.Add(entity);
	}
	
	// REMOVE ENTITY
	public static void RequestRemoveEntity(Entity entity) => RequestRemoveEntityEvent?.Invoke(entity);
	private void RemoveEntity(Entity entity)
	{
		_entitiesToRemove.Add(entity);
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
		if (menu != null) _menusToAdd.Add(menu);
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
