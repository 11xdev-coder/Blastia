using Blastia.Main.Sounds;
using Blastia.Main.Utilities;

namespace Blastia.Main;

public class AudioManager : ManagerWithStateSaving<AudioManager>
{
    public Action? MasterVolumeChanged = MusicEngine.UpdateVolume;
    public Action? MusicVolumeChanged = MusicEngine.UpdateVolume;
    public Action? SoundsVolumeChanged;
    
    protected override string SaveFileName => "audiomanager.bin";
    
    private float _masterVolume = 1;
    public float MasterVolume
    {
        get => _masterVolume;
        set => Properties.OnValueChangedProperty(ref _masterVolume, value, OnMasterVolumeChanged);
    }
    private void OnMasterVolumeChanged() => MasterVolumeChanged?.Invoke();

    private float _musicVolume = 1;
    public float MusicVolume
    {
        get => _musicVolume;
        set => Properties.OnValueChangedProperty(ref _musicVolume, value, OnMusicVolumeChanged);
    }
    private void OnMusicVolumeChanged() => MusicVolumeChanged?.Invoke();
    
    private float _soundsVolume = 1;
    public float SoundsVolume
    {
        get => _soundsVolume;
        set => Properties.OnValueChangedProperty(ref _soundsVolume, value, OnSoundsVolumeChanged);
    }
    private void OnSoundsVolumeChanged() => SoundsVolumeChanged?.Invoke();

    protected override TState GetState<TState>()
    {
        var state =  new AudioManagerState
        {
            MasterVolume = MasterVolume,
            SoundsVolume = SoundsVolume,
            MusicVolume = MusicVolume
        };
        
        return (TState)(object) state;
    }

    protected override void SetState<TState>(TState state)
    {
        if (state is AudioManagerState audioState)
        {
            MasterVolume = audioState.MasterVolume;
            SoundsVolume = audioState.SoundsVolume;
            MusicVolume = audioState.MusicVolume;
        }
        else throw new ArgumentException("Invalid state type. Expected AudioManagerState.");
    }
}

[Serializable]
public class AudioManagerState
{
    public float MasterVolume { get; set; }
    public float SoundsVolume { get; set; }
    public float MusicVolume { get; set; }
}