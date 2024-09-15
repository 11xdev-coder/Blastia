using BlasterMaster.Main.Utilities;

namespace BlasterMaster.Main;

public class AudioManager : Singleton<AudioManager>
{
    private string? _savePath;

    public float MasterVolume = 1;
    public float SoundsVolume = 1;
    public float MusicVolume = 1;

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