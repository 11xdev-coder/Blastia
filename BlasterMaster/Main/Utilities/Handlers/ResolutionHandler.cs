using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Utilities.Handlers;

/// <summary>
/// Gets all supported resolutions and provides CurrentResolution. Has functions like Next() and
/// Previous() that add/subtract current index and wrap around.
/// Also, can be converted ToString()
/// </summary>
public class ResolutionHandler : Handler
{
    private List<DisplayMode> _resolutions;
    private int _currentResolutionIndex;

    public ResolutionHandler()
    {
        _resolutions = VideoManager.Instance.GetSupportedResolutions();
        _currentResolutionIndex = 0;
    }
    
    public DisplayMode CurrentResolution => _resolutions[_currentResolutionIndex];

    public override void Next()
    {
        _currentResolutionIndex = (_currentResolutionIndex + 1) % _resolutions.Count;
    }

    public override void Previous()
    {
        _currentResolutionIndex = (_currentResolutionIndex - 1 + _resolutions.Count) 
                                  % _resolutions.Count;
    }

    public override string GetString() => GetCurrentResolutionWidth() + "x" +
                                         GetCurrentResolutionHeight();
    
    public int GetCurrentResolutionWidth() => CurrentResolution.Width;
    public int GetCurrentResolutionHeight() => CurrentResolution.Height;
}