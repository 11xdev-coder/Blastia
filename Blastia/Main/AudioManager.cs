using Blastia.Main.Sounds;
using Blastia.Main.Utilities;

namespace Blastia.Main;

public class AudioManager : Singleton<AudioManager>
{
    private string? _savePath;

    private float _masterVolume = 1;
    public float MasterVolume
    {
        get => _masterVolume;
        set => Properties.OnValueChangedProperty(ref _masterVolume, value, MusicEngine.UpdateVolume);
    }

    private float _musicVolume = 1;
    public float MusicVolume
    {
        get => _musicVolume;
        set => Properties.OnValueChangedProperty(ref _musicVolume, value, MusicEngine.UpdateVolume);
    }
    
    public float SoundsVolume = 1;

    public void Initialize(string savePath)
    {
        _savePath = savePath;
    }

    private AudioManagerState GetState()
    {
        return new AudioManagerState
        {
            MasterVolume = MasterVolume,
            SoundsVolume = SoundsVolume,
            MusicVolume = MusicVolume
        };
    }

    private void SetState(AudioManagerState state)
    {
        MasterVolume = state.MasterVolume;
        SoundsVolume = state.SoundsVolume;
        MusicVolume = state.MusicVolume;
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
            var state = Saving.Load<AudioManagerState>(_savePath);
            SetState(state);
        }
    }
}

[Serializable]
public class AudioManagerState
{
    public float MasterVolume { get; set; }
    public float SoundsVolume { get; set; }
    public float MusicVolume { get; set; }
}