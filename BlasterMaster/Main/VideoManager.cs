namespace BlasterMaster.Main;

public class VideoManager : Singleton<VideoManager>
{
    public bool IsFullScreen = true;

    public void ToggleFullscreen()
    {
        IsFullScreen = !IsFullScreen;
    }
}