using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace BlasterMaster.Main.Sounds;

public enum SoundID
{
    Tick
}

public static class SoundEngine
{
    private static Dictionary<SoundID, SoundEffect> _sounds = new();
    private static ContentManager? _contentManager;

    public static void Initialize(ContentManager contentManager)
    {
        _contentManager = contentManager;
    }

    public static void LoadSounds()
    {
        LoadSound(SoundID.Tick, Paths.TickSoundPath);
    }

    public static void LoadSound(SoundID id, string path)
    {
        if (_contentManager == null) return;
        
        try
        {
            _sounds[id] = _contentManager.Load<SoundEffect>(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sound with ID: {id}. Err: {ex.Message}");
        }
    }

    public static void PlaySound(SoundID id)
    {
        if (_sounds.TryGetValue(id, out var sound))
        {
            float volume = AudioManager.Instance.SoundsVolume * AudioManager.Instance.MasterVolume;
            sound.Play(volume, 0f, 0f);
        }
        else
        {
            Console.WriteLine($"Sound with ID: {id} not found.");
        }
    }

    public static void UnloadSounds()
    {
        foreach (var sound in _sounds.Values)
        {
            sound.Dispose();
        }
        _sounds.Clear();
    }
}