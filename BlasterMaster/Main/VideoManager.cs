using BlasterMaster.Main.Utilities;
using BlasterMaster.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main;

public class VideoManager : Singleton<VideoManager>
{
    private GraphicsDeviceManager? _graphics;
    private string? _savePath;
    
    // if graphics are null -> false
    // property will update on itself
    public bool IsFullScreen => _graphics?.IsFullScreen ?? false;
    
    private ResolutionListHandler? _resolutionHandler;
    public ResolutionListHandler ResolutionHandler
    {
        get
        {
            if (_resolutionHandler == null)
                throw new Exception("Resolution handler is not initialized.");
            
            return _resolutionHandler;
        }
        private set => _resolutionHandler = value;
    }

    public void Initialize(GraphicsDeviceManager graphics, string savePath)
    {
        _graphics = graphics;
        _savePath = savePath;
        
        ResolutionHandler = new ResolutionListHandler();
    }
    
    public void ToggleFullscreen()
    {
        if (_graphics != null)
        {
            _graphics.ToggleFullScreen();
        }
    }

    public void SetFullscreen(bool val)
    {
        if (_graphics != null)
        {
            _graphics.IsFullScreen = val;
            _graphics.ApplyChanges();
        }
    }

    public void ApplyHandlerResolution()
    {
        if (_graphics != null)
        {
            _graphics.PreferredBackBufferWidth = ResolutionHandler.GetCurrentResolutionWidth();
            _graphics.PreferredBackBufferHeight = ResolutionHandler.GetCurrentResolutionHeight();
            _graphics.ApplyChanges();
        }
    }

    public List<DisplayMode> GetSupportedResolutions()
    {
        List<DisplayMode> resolutions = new List<DisplayMode>();
        foreach (var mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
        {
            resolutions.Add(mode);
        }
        return resolutions;
    }

    public VideoManagerState GetState()
    {
        return new VideoManagerState
        {
            IsFullScreen = this.IsFullScreen
        };
    }

    public void SetState(VideoManagerState state)
    {
        SetFullscreen(state.IsFullScreen);
    }

    public void SaveStateToFile()
    {
        if (_savePath != null)
        {
            var state = GetState();
            Saving.Save(_savePath, state);
        }
    }

    public void LoadStateFromFile()
    {
        if (_savePath != null)
        {
            var state = Saving.Load<VideoManagerState>(_savePath);
            SetState(state);
        }
    }
}

[Serializable]
public class VideoManagerState
{
    public bool IsFullScreen { get; set; }
}