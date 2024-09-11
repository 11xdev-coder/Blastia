using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;

namespace BlasterMaster.Main;

public class VideoManager : Singleton<VideoManager>
{
    private GraphicsDeviceManager? _graphics;
    private string? _savePath;
    
    // if graphics are null -> false
    // property will update on itself
    public bool IsFullScreen => _graphics?.IsFullScreen ?? false;

    public void Initialize(GraphicsDeviceManager graphics, string savePath)
    {
        _graphics = graphics;
        _savePath = savePath;
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