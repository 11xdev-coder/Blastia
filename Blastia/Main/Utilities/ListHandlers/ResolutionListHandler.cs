using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Utilities.ListHandlers;

/// <summary>
/// Gets all supported resolutions and provides CurrentResolution. Has functions like Next() and
/// Previous() that add/subtract current index and wrap around.
/// Also, can be converted ToString()
/// </summary>
public class ResolutionListHandler() : ListHandler<DisplayMode>(VideoManager.Instance.GetSupportedResolutions())
{
    public DisplayMode CurrentResolution => CurrentItem;

    public override string GetString() => GetCurrentResolutionWidth() + "x" +
                                         GetCurrentResolutionHeight();
    
    public int GetCurrentResolutionWidth() => CurrentResolution.Width;
    public int GetCurrentResolutionHeight() => CurrentResolution.Height;
}