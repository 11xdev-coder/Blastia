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
	public static string ProgrssbarBackgroundPath { get; private set; } = "";
	public static string WhitePixelPath { get; private set; } = "";
	public static string InvisibleTexturePath { get; private set; } = "";

	// player textures
	public static string PlayerHeadTexturePath { get; private set; } = "";
	public static string PlayerBodyTexturePath { get; private set; } = "";
	public static string PlayerLeftArmTexturePath { get; private set; } = "";
	public static string PlayerRightArmTexturePath { get; private set; } = "";
	public static string PlayerLegTexturePath { get; private set; } = "";
	
	// sounds
	public static readonly string TickSoundPath = "Sounds/Menu/Tick.wav";
	
	// music
	public static readonly string PeacefulJourney00 = "Sounds/MenuSongs/peaceful journey_00.ogg";
	public static readonly string PeacefulJourney01 = "Sounds/MenuSongs/peaceful journey_01.ogg";
	public static readonly string PeacefulJourney02 = "Sounds/MenuSongs/peaceful journey_02.ogg";
	public static readonly string PeacefulJourney03 = "Sounds/MenuSongs/peaceful journey_03.ogg";

	// settings and save paths
	public static string VideoManagerSavePath { get; private set; } = "";
	public static string AudioManagerSavePath { get; private set; } = "";
	public static string PlayerSavePath { get; private set; } = "";
	public static string WorldsSavePath { get; private set; } = "";
	
	// loading
	public static string BlockTextures { get; private set; } = "";
	
	// update all paths when ContentRoot changes
	private static void UpdatePaths()
	{
		CursorTexturePath = Path.Combine(ContentRoot, "Textures/Cursor.png");
		Logo5XTexturePath = Path.Combine(ContentRoot, "Textures/Menu/Logo5X.png");
		SliderBackgroundPath = Path.Combine(ContentRoot, "Textures/UI/SliderBG.png");
		ProgrssbarBackgroundPath = Path.Combine(ContentRoot, "Textures/UI/ProgressBarBG.png");
		WhitePixelPath = Path.Combine(ContentRoot, "Textures/WhitePixel.png");
		InvisibleTexturePath = Path.Combine(ContentRoot, "Textures/Invisible.png");

		// Player textures
		PlayerHeadTexturePath = Path.Combine(ContentRoot, "Textures/Player/Head.png");
		PlayerBodyTexturePath = Path.Combine(ContentRoot, "Textures/Player/Body.png");
		PlayerLeftArmTexturePath = Path.Combine(ContentRoot, "Textures/Player/LeftArm.png");
		PlayerRightArmTexturePath = Path.Combine(ContentRoot, "Textures/Player/RightArm.png");
		PlayerLegTexturePath = Path.Combine(ContentRoot, "Textures/Player/Leg.png");

		// Save paths
		VideoManagerSavePath = ContentRoot + "/Saved/videomanager.bin";
		AudioManagerSavePath = ContentRoot + "/Saved/audiomanager.bin";
		PlayerSavePath = ContentRoot + "/Saved/Players/";
		WorldsSavePath = ContentRoot + "/Saved/Worlds/";
		
		// loading
		BlockTextures = ContentRoot + "/Textures/Blocks";
	}
	
	// Windows -> Documents/My Games/Blastia
	// Linux -> ~/.local/share/Blastia
	// Mac -> ~/Library/Application Support/Blastia
	public static string GetSaveGameDirectory() 
	{
		string basePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		return basePath;
	}
}