using System.IO.Pipes;
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
                throw new NullReferenceException("Resolution handler is not initialized.");
            
            return _resolutionHandler;
        }
        private set => _resolutionHandler = value;
    }
    
    public readonly Vector2 TargetResolution = new (1920, 1080);

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

    private void SetResolutionByIndex(int index)
    {
        if (_graphics != null)
        {
            ResolutionHandler.CurrentIndex = index;
        }
    }

    public void ApplyHandlerResolution()
    {
        if (_graphics != null)
        {
            var x = ResolutionHandler.GetCurrentResolutionWidth();
            var y = ResolutionHandler.GetCurrentResolutionHeight();
            _graphics.PreferredBackBufferWidth = x;
            _graphics.PreferredBackBufferHeight = y;
            _graphics.ApplyChanges();
            
            BlasterMasterGame.RequestResolutionUpdate();
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
            IsFullScreen = IsFullScreen,
            ResolutionIndex = ResolutionHandler.CurrentIndex
        };
    }

    public void SetState(VideoManagerState state)
    {
        SetFullscreen(state.IsFullScreen);
        SetResolutionByIndex(state.ResolutionIndex);
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

    public Matrix CalculateResolutionScaleMatrix()
    {
        if (_graphics != null)
        {
            BlasterMasterGame.RequestResolutionUpdate();
            float scaleX = _graphics.PreferredBackBufferWidth / TargetResolution.X;
            float scaleY = _graphics.PreferredBackBufferHeight / TargetResolution.Y;
            return Matrix.CreateScale(scaleX, scaleY, 1f);
        }
        return Matrix.CreateScale(1f, 1f, 1f);
    }
}

[Serializable]
public class VideoManagerState
{
    public bool IsFullScreen { get; set; }
    public int ResolutionIndex { get; set; }
}