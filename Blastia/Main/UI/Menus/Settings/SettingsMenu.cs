using Blastia.Main.Sounds;
using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.Settings;

public enum SettingsTab 
{
    None,
    Audio,
    Video
}

public class SettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Dictionary<SettingsTab, List<UIElement>> _tabElements = [];
    private List<UIElement> _sharedElements = [];
    private SettingsTab _activeTab = SettingsTab.None;
    private SettingsTab _pendingTab = SettingsTab.None;
    
    private const int Width = 1300;
    private const int Height = 600;
    private const int Offset = 200;
    private const float FirstElementY = -Height * 0.5f - 40;
    
    /// <summary>
    /// Initializes tab in dictionary and adds an element to it
    /// </summary>
    private void AddTo(SettingsTab tab, UIElement e) 
    {
        if (!_tabElements.ContainsKey(tab))
        {
            _tabElements.Add(tab, []);
        }
        
        _tabElements[tab].Add(e);
    }
    
    protected override void AddElements()
    {
        AdvancedBackground bg = new AdvancedBackground(Vector2.Zero, Width, Height, Colors.DarkBackground, 2, Colors.DarkBorder) 
        {
            HAlign = 0.5f,  
            VAlign = 0.65f
        };
        _sharedElements.Add(bg);
        
        // ------------------------ AUDIO -----------------------------------
        Button audioTab = new Button(new Vector2(-Width * 0.5f + Font.MeasureString("Audio").X, FirstElementY), "Audio", Font, () => RequestTab(SettingsTab.Audio, () => AudioManager.Instance.LoadStateFromFile<AudioManagerState>())) 
        {
            HAlign = 0.5f,
            VAlign = 0.65f
        };
        _sharedElements.Add(audioTab);
        
        Button randomMusicButton = new Button(new Vector2(Offset, FirstElementY), "Play random menu track", Font, PlayRandomMusic)
        {
            HAlign = 0.5f,
            VAlign = 0.65f
        };
        AddTo(SettingsTab.Audio, randomMusicButton);
        
        var master = AddMasterVolumeSlider(new Vector2(Offset, FirstElementY + 50), 0.5f, 0.65f);
        AddTo(SettingsTab.Audio, master.text);
        AddTo(SettingsTab.Audio, master.slider);
        var music = AddMusicVolumeSlider(new Vector2(Offset, FirstElementY + 100), 0.5f, 0.65f);
        AddTo(SettingsTab.Audio, music.text);
        AddTo(SettingsTab.Audio, music.slider);
        var fx = AddSoundVolumeSlider(new Vector2(Offset, FirstElementY + 150), 0.5f, 0.65f);
        AddTo(SettingsTab.Audio, fx.text);
        AddTo(SettingsTab.Audio, fx.slider);
        
        // ----------------------------- VIDEO ----------------------------------
        Button videoTab = new Button(new Vector2(-Width * 0.5f + Font.MeasureString("Audio").X, FirstElementY + 50), "Video", Font, () => RequestTab(SettingsTab.Video, () => VideoManager.Instance.LoadStateFromFile<VideoManagerState>())) 
        {
            HAlign = 0.5f,
            VAlign = 0.65f
        };
        _sharedElements.Add(videoTab);
        
        var fullscreen = AddFullscreenSwitch(new Vector2(Offset, FirstElementY), 0.5f, 0.65f, () => {});
        AddTo(SettingsTab.Video, fullscreen);
        
        var resolution = AddResolutionHandler(new Vector2(Offset, FirstElementY + 50), 0.5f, 0.65f, VideoManager.Instance.ApplyHandlerResolution);
        AddTo(SettingsTab.Video, resolution);
        
        Elements.AddRange(_sharedElements);
    }
    
    private void PlayRandomMusic()
    {
        MusicID musicId = BlastiaGame.ChooseRandomMenuMusic();
        MusicEngine.PlayMusicTrack(musicId);
    }

    private void OnClickBack()
    {
        SwitchToMenu(BlastiaGame.GetMenu<MainMenu>());
        AudioManager.Instance.SaveStateToFile<AudioManagerState>();
        VideoManager.Instance.SaveStateToFile<VideoManagerState>();
    }
    
    /// <summary>
    /// Requests to show a tab. Must be called first to avoid list modification errors
    /// </summary>
    private void RequestTab(SettingsTab tab, Action loadManagerState) 
    {
        if (_pendingTab == tab) return;
        
        loadManagerState();
        _pendingTab = tab;
    }
    
    private void ShowTab(SettingsTab tab) 
    {
        if (_activeTab == tab) return;
        
        _activeTab = tab;
        Elements.Clear();
        Elements.AddRange(_sharedElements);
        if (_tabElements.TryGetValue(tab, out var elems)) 
        {
            Elements.AddRange(elems);
        }
        else { Console.WriteLine($"[Settings] Tab is not initialized, but tried to show"); }
    }

    public override void Update()
    {
        // if we requested to change the tab, do it here to avoid modification error
        if (_pendingTab != SettingsTab.None) 
        {
            ShowTab(_pendingTab);
            _pendingTab = SettingsTab.None;
        }
        base.Update();
    }
}