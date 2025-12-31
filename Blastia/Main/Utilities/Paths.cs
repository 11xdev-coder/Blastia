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
