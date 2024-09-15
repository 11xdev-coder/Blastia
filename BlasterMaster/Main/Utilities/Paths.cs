namespace BlasterMaster.Main.Utilities;

/// <summary>
/// Stores various paths, texture paths should be added to Content.RootDirectory
/// </summary>
public static class Paths
{
    // textures should be added to roots
    public static readonly string CursorTexturePath = "/Textures/Cursor.png";
    public static readonly string Logo5XTexturePath = "/Textures/Menu/Logo5X.png";
    public static readonly string SliderBackgroundPath = "/Textures/UI/SliderBG.png";
    
    // sounds
    public static readonly string TickSoundPath = "Sounds/Menu/Tick.wav";

    public static readonly string VideoManagerSavePath = "/Saved/videomanager.bin";
    public static readonly string AudioManagerSavePath = "/Saved/audiomanager.bin";
}