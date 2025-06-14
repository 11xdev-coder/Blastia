using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Blastia.Main.Sounds;

public enum SoundID
{
    Tick,
    Grab,
    Dig1,
    Dig2,
    Dig3,
    Tink1,
    Tink2,
    Tink3
}

public static class SoundEngine
{
    private static Dictionary<SoundID, SoundEffect> _sounds = new();
    private static ContentManager? _contentManager;

    private static Dictionary<Vector2, SoundEffectInstance> _activeBlockSounds = [];

    public static void Initialize(ContentManager contentManager)
    {
        _contentManager = contentManager;
    }

    public static void LoadSounds()
    {
        LoadSound(SoundID.Tick, Paths.TickSoundPath);
        LoadSound(SoundID.Grab, Paths.GrabPath);
        LoadSound(SoundID.Dig1, Paths.Dig1Path);
        LoadSound(SoundID.Dig2, Paths.Dig2Path);
        LoadSound(SoundID.Dig3, Paths.Dig3Path);
        LoadSound(SoundID.Tink1, Paths.Tink1Path);
        LoadSound(SoundID.Tink2, Paths.Tink2Path);
        LoadSound(SoundID.Tink3, Paths.Tink3Path);
    }

    private static void LoadSound(SoundID id, string path)
    {
        if (_contentManager == null) return;
        
        try
        {
            _sounds[id] = _contentManager.Load<SoundEffect>(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sound. ID: {id}, Exception: {ex.Message}");
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

    /// <summary>
    /// Plays the sound and saves a <c>SoundEffectInstance</c> to avoid sound overlapping for this block position
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position"></param>
    public static void PlaySoundWithoutOverlappingForBlock(SoundID id, Vector2 position)
    {
        StopSoundsForBlock(position);

        if (_sounds.TryGetValue(id, out var sound))
        {
            var volume = AudioManager.Instance.SoundsVolume * AudioManager.Instance.MasterVolume;
            var soundInstance = sound.CreateInstance();
            soundInstance.Volume = volume;
            soundInstance.Play();
            
            _activeBlockSounds[position] = soundInstance;
        }
    }

    /// <summary>
    /// Use in pair with <see cref="PlaySoundWithoutOverlappingForBlock"/>. Stops active sounds for this block position
    /// </summary>
    /// <param name="position"></param>
    public static void StopSoundsForBlock(Vector2 position)
    {
        if (_activeBlockSounds.TryGetValue(position, out var sound))
        {
            sound.Stop();
            sound.Dispose();
            _activeBlockSounds.Remove(position);
        }
    }
    
    /// <summary>
    /// Use in pair with <see cref="PlaySoundWithoutOverlappingForBlock"/>. Checks if any sound is playing for this block position
    /// </summary>
    /// <param name="position"></param>
    public static bool IsSoundPlayingForBlock(Vector2 position)
    {
        if (_activeBlockSounds.TryGetValue(position, out var sound))
        {
            return sound.State == SoundState.Playing;
        }

        return false;
    }

    /// <summary>
    /// Cleans up finished sounds
    /// </summary>
    public static void Update()
    {
        // clean up
        var finishedBlockSoundPositions = new List<Vector2>();
        foreach (var kvp in _activeBlockSounds)
        {
            if (kvp.Value.State == SoundState.Stopped)
            {
                finishedBlockSoundPositions.Add(kvp.Key);
            }
        }

        foreach (var position in finishedBlockSoundPositions)
        {
            _activeBlockSounds[position].Dispose();
            _activeBlockSounds.Remove(position);
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