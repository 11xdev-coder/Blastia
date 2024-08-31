using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace BlasterMaster.Main.Sounds;

public enum SoundID
{
    Tick
}

public class SoundEngine
{
    public static SoundEngine Instance { get; private set; }
    
    private Dictionary<SoundID, SoundEffect> _sounds;
    private ContentManager _contentManager;

    public SoundEngine(ContentManager contentManager)
    {
        Instance = this;
        
        _sounds = new Dictionary<SoundID, SoundEffect>();
        _contentManager = contentManager;
    }

    public void LoadSounds()
    {
        LoadSound(SoundID.Tick, Paths.TickSoundPath);
    }

    public void LoadSound(SoundID id, string path)
    {
        try
        {
            _sounds[id] = _contentManager.Load<SoundEffect>(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sound with ID: {id}. Err: {ex.Message}");
        }
    }

    public void PlaySound(SoundID id)
    {
        if (_sounds.TryGetValue(id, out var sound))
        {
            sound.Play();
        }
        else
        {
            Console.WriteLine($"Sound with ID: {id} not found.");
        }
    }

    public void UnloadSounds()
    {
        foreach (var sound in _sounds.Values)
        {
            sound.Dispose();
        }
        _sounds.Clear();
    }
}