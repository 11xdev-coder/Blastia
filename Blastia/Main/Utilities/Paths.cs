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
	
	// sounds
	public static readonly string TickSoundPath = "Sounds/Menu/Tick.wav";
	
	// music
	public static readonly string PeacefulJourney00 = "Sounds/MenuSongs/peaceful journey_00.ogg";
	public static readonly string PeacefulJourney01 = "Sounds/MenuSongs/peaceful journey_01.ogg";
	public static readonly string PeacefulJourney02 = "Sounds/MenuSongs/peaceful journey_02.ogg";
	public static readonly string PeacefulJourney03 = "Sounds/MenuSongs/peaceful journey_03.ogg";
	
	// loading
	public static string BlockTextures { get; private set; } = "";
	public static string HumanTextures { get; private set; } = "";
	
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
		
		// loading
		BlockTextures = ContentRoot + "/Textures/Blocks";
		HumanTextures = ContentRoot + "/Textures/Entities/Humans";
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