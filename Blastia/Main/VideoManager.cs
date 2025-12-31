using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main;

public class VideoManager : ManagerWithStateSaving<VideoManager>
{
    public Action? FullScreenChanged;
    private void OnPropertyUpdated() => FullScreenChanged?.Invoke();
    
    private GraphicsDeviceManager? _graphics;
    protected override string SaveFileName => "videomanager.bin";
    
    // if graphics are null -> false
    // property will update on itself
    private bool _isFullScreen;
    public bool IsFullScreen
    {
        get => _isFullScreen;
        private set => Properties.OnValueChangedProperty(ref _isFullScreen, value, OnPropertyUpdated);
    }
    
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
    public Action? ResolutionChanged;
    private void OnResolutionChanged() => ResolutionChanged?.Invoke();
    
    public readonly Vector2 TargetResolution = new (1920, 1080);

    public void Initialize(GraphicsDeviceManager graphics)
    {
        base.Initialize();
        
        _graphics = graphics;
        IsFullScreen = graphics.IsFullScreen;
        
        ResolutionHandler = new ResolutionListHandler();
        ResolutionHandler.IndexChanged += OnResolutionChanged;
    }
    
    public void ToggleFullscreen()
    {
        if (_graphics != null)
        {
            _graphics.ToggleFullScreen();
            IsFullScreen = !IsFullScreen;
        }
    }

    public void SetFullscreen(bool val)
    {
        if (_graphics != null)
        {
            _graphics.IsFullScreen = val;
            _graphics.ApplyChanges();
            
            IsFullScreen = val;
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
            
            BlastiaGame.RequestResolutionUpdate();
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

    protected override TState GetState<TState>()
    {
        var state =  new VideoManagerState
        {
            IsFullScreen = IsFullScreen,
            ResolutionIndex = ResolutionHandler.CurrentIndex
        };
        
        return (TState)(object) state;
    }

    protected override void SetState<TState>(TState state)
    {
        if (state is VideoManagerState videoState)
        {
            SetFullscreen(videoState.IsFullScreen);
            SetResolutionByIndex(videoState.ResolutionIndex);
        }
        else throw new ArgumentException("Invalid state type. Expected VideoManagerState.");
    }
    
    public Matrix CalculateResolutionScaleMatrix()
    {
        if (_graphics != null)
        {
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