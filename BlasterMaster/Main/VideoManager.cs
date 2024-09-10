using Microsoft.Xna.Framework;

namespace BlasterMaster.Main;

public class VideoManager : Singleton<VideoManager>
{
    private GraphicsDeviceManager? _graphics;
    
    // if graphics are null -> false
    // property will update on itself
    public bool IsFullScreen => _graphics?.IsFullScreen ?? false;

    public void Initialize(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;
    }
    
    public void ToggleFullscreen()
    {
        if (_graphics != null)
        {
            _graphics.ToggleFullScreen();
        }
    }
}