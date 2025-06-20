namespace Blastia.Main.Utilities;

/// <summary>
/// Stores various paths, texture paths should be added to Content.RootDirectory
/// </summary>
public static class Paths
{
	private static readonly string GameName = "Blastia";
	
	private static string _contentRoot = "";
	public static string ContentRoot
	{
		get => _contentRoot;
		set => Properties.OnValueChangedProperty(ref _contentRoot, value, UpdatePaths);
	}
	
	// relative paths
	public static string CursorTexturePath { get; private set; } = "";
	public static string Logo5XTexturePath { get; private set; } = "";
	public static string SliderBackgroundPath { get; private set; } = "";
	public static string RulerBlockHighlightPath { get; private set; } = "";
	public static string ProgressbarBackgroundPath { get; private set; } = "";
	public static string WhitePixelPath { get; private set; } = "";
	public static string InvisibleTexturePath { get; private set; } = "";
	public static string MonitorTexturePath { get; private set; } = "";
	public static string AudioTexturePath { get; private set; } = "";
	public static string RedCrossPath { get; private set; } = "";
	public static string ExitPath { get; private set; } = "";
	public static string SlotBackgroundTexturePath { get; private set; } = "";
	public static string SlotHighlightedTexturePath { get; private set; } = "";
	public static string BlockDestroyTexturePath { get; private set; } = "";
	public static string SignEditBackgroundTexturePath { get; private set; } = "";
	public static string SignWrittenOverlay1TexturePath { get; private set; } = "";
	public static string SignWrittenOverlay2TexturePath { get; private set; } = "";
	
	// sounds
	public static readonly string TickSoundPath = "Sounds/Menu/Tick.wav";
	public static readonly string GrabPath = "Sounds/Game/Grab.wav";
	public static readonly string Dig1Path = "Sounds/Game/Dig1.wav";
	public static readonly string Dig2Path = "Sounds/Game/Dig2.wav";
	public static readonly string Dig3Path = "Sounds/Game/Dig3.wav";
	public static readonly string Tink1Path = "Sounds/Game/Tink1.wav";
	public static readonly string Tink2Path = "Sounds/Game/Tink2.wav";
	public static readonly string Tink3Path = "Sounds/Game/Tink3.wav";
	
	// music
	public static readonly string RatsOnSaturn = "Sounds/MenuSongs/rats_on_saturn.ogg";
	public static readonly string PeacefulJourney00 = "Sounds/MenuSongs/peaceful journey_00.ogg";
	public static readonly string PeacefulJourney01 = "Sounds/MenuSongs/peaceful journey_01.ogg";
	public static readonly string PeacefulJourney02 = "Sounds/MenuSongs/peaceful journey_02.ogg";
	public static readonly string PeacefulJourney03 = "Sounds/MenuSongs/peaceful journey_03.ogg";
	
	// loading
	public static string BlockTextures { get; private set; } = "";
	public static string HumanTextures { get; private set; } = "";
	public static string BlocksData { get; private set; } = "";
	public static string ItemsData { get; private set; } = "";
	
	// update all paths when ContentRoot changes
	private static void UpdatePaths()
	{
		CursorTexturePath = Path.Combine(ContentRoot, "Textures/Cursor.png");
		Logo5XTexturePath = Path.Combine(ContentRoot, "Textures/Menu/Logo5X.png");
		SliderBackgroundPath = Path.Combine(ContentRoot, "Textures/UI/SliderBG.png");
		RulerBlockHighlightPath = Path.Combine(ContentRoot, "Textures/UI/RulerBlockHighlight.png");
		ProgressbarBackgroundPath = Path.Combine(ContentRoot, "Textures/UI/ProgressBarBG.png");
		WhitePixelPath = Path.Combine(ContentRoot, "Textures/WhitePixel.png");
		InvisibleTexturePath = Path.Combine(ContentRoot, "Textures/Invisible.png");
		MonitorTexturePath = Path.Combine(ContentRoot, "Textures/UI/Monitor.png");
		AudioTexturePath = Path.Combine(ContentRoot, "Textures/UI/Audio.png");
		RedCrossPath = Path.Combine(ContentRoot, "Textures/UI/RedCross.png");
		ExitPath = Path.Combine(ContentRoot, "Textures/UI/Exit.png");
		SlotBackgroundTexturePath = Path.Combine(ContentRoot, "Textures/UI/SlotBackground.png");
		SlotHighlightedTexturePath = Path.Combine(ContentRoot, "Textures/UI/SlotHighlighted.png");
		BlockDestroyTexturePath = Path.Combine(ContentRoot, "Textures/Blocks/BlockDestroy.png");
		SignEditBackgroundTexturePath = Path.Combine(ContentRoot, "Textures/UI/SignEditBackground.png");
		SignWrittenOverlay1TexturePath = Path.Combine(ContentRoot, "Textures/Blocks/SignWrittenOverlay1.png");
		SignWrittenOverlay2TexturePath = Path.Combine(ContentRoot, "Textures/Blocks/SignWrittenOverlay2.png");
		
		// loading
		BlockTextures = ContentRoot + "/Textures/Blocks";
		HumanTextures = ContentRoot + "/Textures/Entities/Humans";
		BlocksData = ContentRoot + "/Data/blocks.json";
		ItemsData = ContentRoot + "/Data/items.json";
	}
	
	// Windows -> Documents/My Games/Blastia
	// Linux -> ~/.local/share/Blastia
	// Mac -> ~/Library/Application Support/Blastia
	public static string GetSaveGameDirectory()
	{
		string gameSavePath;
		
		if (OperatingSystem.IsWindows())
		{
			string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			gameSavePath = Path.Combine(documentsPath, "My Games", GameName);
		}
		else if (OperatingSystem.IsLinux())
		{
			string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			gameSavePath =  Path.Combine(localAppDataPath, GameName);
		}
		else if (OperatingSystem.IsMacOS())
		{
			string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string libraryPath = Path.Combine(userProfilePath, "Library", "Application Support");
			gameSavePath = Path.Combine(libraryPath, GameName);
		}
		else
		{
			throw new PlatformNotSupportedException("Platform not supported");
		}

		if (!Directory.Exists(gameSavePath))
		{
			Directory.CreateDirectory(gameSavePath);
		}
		
		return gameSavePath;
	}
}
