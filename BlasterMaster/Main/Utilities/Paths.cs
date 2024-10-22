namespace BlasterMaster.Main.Utilities;

/// <summary>
/// Stores various paths, texture paths should be added to Content.RootDirectory
/// </summary>
public static class Paths
{
	private static string contentRoot = "";
	public static string ContentRoot
	{
		get => contentRoot;
		set
		{
			contentRoot = value;
			UpdatePaths();
		}
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
	
	// path bases
	private static class RelativePaths
	{
		public const string CursorTexture = "Textures/Cursor.png";
		public const string Logo5XTexture = "Textures/Menu/Logo5X.png";
		public const string SliderBackground = "Textures/UI/SliderBG.png";
		public const string ProgressbarBackground = "Textures/UI/ProgressBarBG.png";
		public const string WhitePixel = "Textures/WhitePixel.png";
		public const string InvisibleTexture = "Textures/Invisible.png";

		public static class Player
		{
			public const string Head = "Textures/Player/Head.png";
			public const string Body = "Textures/Player/Body.png";
			public const string LeftArm = "Textures/Player/LeftArm.png";
			public const string RightArm = "Textures/Player/RightArm.png";
			public const string Leg = "Textures/Player/Leg.png";
		}
	}
	
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
	
	// update all paths when ContentRoot changes
	private static void UpdatePaths()
	{
		CursorTexturePath = Path.Combine(ContentRoot, RelativePaths.CursorTexture);
		Logo5XTexturePath = Path.Combine(ContentRoot, RelativePaths.Logo5XTexture);
		SliderBackgroundPath = Path.Combine(ContentRoot, RelativePaths.SliderBackground);
		ProgrssbarBackgroundPath = Path.Combine(ContentRoot, RelativePaths.ProgressbarBackground);
		WhitePixelPath = Path.Combine(ContentRoot, RelativePaths.WhitePixel);
		InvisibleTexturePath = Path.Combine(ContentRoot, RelativePaths.InvisibleTexture);

		// Player textures
		PlayerHeadTexturePath = Path.Combine(ContentRoot, RelativePaths.Player.Head);
		PlayerBodyTexturePath = Path.Combine(ContentRoot, RelativePaths.Player.Body);
		PlayerLeftArmTexturePath = Path.Combine(ContentRoot, RelativePaths.Player.LeftArm);
		PlayerRightArmTexturePath = Path.Combine(ContentRoot, RelativePaths.Player.RightArm);
		PlayerLegTexturePath = Path.Combine(ContentRoot, RelativePaths.Player.Leg);

		// Save paths
		VideoManagerSavePath = ContentRoot + "/Saved/videomanager.bin";
		AudioManagerSavePath = ContentRoot + "/Saved/audiomanager.bin";
		PlayerSavePath = ContentRoot + "/Saved/Players/";
		WorldsSavePath = ContentRoot + "/Saved/Worlds/";
	}
}