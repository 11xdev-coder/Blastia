using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Blastia.Main.Sounds;

public struct DelayInfo
{
    public float DelayEndTime;
    public float DelayDuration;

    public DelayInfo(float delayEndTime, float delayDuration)
    {
        DelayEndTime = delayEndTime;
        DelayDuration = delayDuration;
    }
}

public enum SoundID
{
    Tick,
    Grab,
    Dig1,
    Dig2,
    Dig3,
    Tink1,
    Tink2,
    Tink3,
    FleshHit,
    PlayerDeath
}

public enum BlockMaterial 
{
    Stone,
    Dirt,
    Wood,
    Metal,
    Glass,
    Sand,
    Gravel
}

public class ProceduralSoundGenerator 
{
    private const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private const int Channels = 1;
    private Random _random = new();
    
    public SoundEffect GenerateBlockBreakSound(BlockMaterial material, float hardness) 
    {
        var duration = (int)(0.3 * SampleRate);
        byte[] audioData = new byte[duration * 2];
        
        for (int i = 0; i < duration; i++) 
        {
            var time = i / (float)SampleRate;
            var sample = 0f;

            // base impact
            var impact = (float)(_random.NextDouble() * 2 - 1);
            impact *= MathF.Exp(-time * 20);

            // material specific frequency
            var frequency = material switch
            {
                BlockMaterial.Stone => 200 + hardness * 300,
                BlockMaterial.Metal => 800 + hardness * 500,
                BlockMaterial.Wood => 400 + hardness * 200,
                BlockMaterial.Glass => 1200 + hardness * 800,
                BlockMaterial.Dirt => 150 + hardness * 100,
                BlockMaterial.Sand => 100 + hardness * 80,
                BlockMaterial.Gravel => 250 + hardness * 150,
                _ => 300
            };
            
            // tonal component
            if (material == BlockMaterial.Metal || material == BlockMaterial.Glass) 
            {
                var tone = MathF.Sin(2 * MathF.PI * frequency * time);
                tone *= MathF.Exp(-time * 10);
                sample += tone * 0.3f;
            }

            // cracking sounds (multiple micro-impacts)
            var crackCount = material == BlockMaterial.Stone ? 5 : 3;
            for (int c = 0; c < crackCount; c++) 
            {
                var crackTime = c * 0.05f;
                if (time > crackTime && time < crackTime + 0.01f) 
                {
                    var crack = (float)(_random.NextDouble() * 2 - 1);
                    crack *= 0.5f;
                    sample += crack;
                }
            }

            sample += impact;

            // low-pass filter
            var cutoff = frequency / (SampleRate / 2);
            sample *= MathF.Min(1, cutoff * 2);

            // clamp and convert to 16-bit
            sample = Math.Clamp(sample, -1f, 1f);
            var value = (short)(sample * short.MaxValue);
            audioData[i * 2] = (byte)(value & 0xFF);
            audioData[i * 2 + 1] = (byte)(value >> 8);
        }

        return new SoundEffect(audioData, SampleRate, (AudioChannels)Channels);
    }
}

public static class SoundEngine
{
    private static Dictionary<SoundID, SoundEffect> _sounds = new();
    private static ContentManager? _contentManager;
    
    #region Procedural Sounds
    private static ProceduralSoundGenerator _soundGenerator = new();
    private static Dictionary<string, SoundEffect> _proceduralSoundCache = new();
    private const int MaxCacheSize = 50;    
    
    #endregion

    private static readonly Dictionary<Vector2, SoundEffectInstance> ActiveBlockSounds = [];
    private static readonly Dictionary<Vector2, DelayInfo> BlockSoundDelays = [];

    public static void Initialize(ContentManager contentManager)
    {
        _contentManager = contentManager;
    }

    private static string GetPath(string folder, string name) => $"Sounds/{folder}/{name}.wav";

    public static void LoadSounds()
    {
        LoadSound(SoundID.Tick, GetPath("Menu", "Tick"));
        LoadSound(SoundID.Grab, GetPath("Game", "Grab"));
        LoadSound(SoundID.Dig1, GetPath("Game", "Dig1"));
        LoadSound(SoundID.Dig2, GetPath("Game", "Dig2"));
        LoadSound(SoundID.Dig3, GetPath("Game", "Dig3"));
        LoadSound(SoundID.Tink1, GetPath("Game", "Tink1"));
        LoadSound(SoundID.Tink2, GetPath("Game", "Tink2"));
        LoadSound(SoundID.Tink3, GetPath("Game", "Tink3"));
        LoadSound(SoundID.FleshHit, GetPath("Game", "FleshHit"));
        LoadSound(SoundID.PlayerDeath, GetPath("Game", "PlayerDeath"));
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

    #region Normal Sounds
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
    /// <param name="delayAfterSound">How much time to wait before <see cref="CanPlaySoundForBlock"/> method will return true</param>
    public static void PlaySoundWithoutOverlappingForBlock(SoundID id, Vector2 position, float delayAfterSound = 0.5f)
    {
        StopSoundsForBlock(position);

        if (_sounds.TryGetValue(id, out var sound))
        {
            var volume = AudioManager.Instance.SoundsVolume * AudioManager.Instance.MasterVolume;
            var soundInstance = sound.CreateInstance();
            soundInstance.Volume = volume;
            soundInstance.Play();
            
            ActiveBlockSounds[position] = soundInstance;
            
            // delay
            var currentTime = (float) BlastiaGame.GameTime.TotalGameTime.TotalSeconds;
            var soundDuration = (float) sound.Duration.TotalSeconds;
            var delayEndTime = currentTime + soundDuration + delayAfterSound;
            BlockSoundDelays[position] = new DelayInfo(delayEndTime, delayAfterSound);
        }
    }
    
    /// <summary>
    /// Plays the provided sound and saves a <c>SoundEffectInstance</c> to avoid sound overlapping for this block position
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position"></param>
    /// <param name="delayAfterSound">How much time to wait before <see cref="CanPlaySoundForBlock"/> method will return true</param>
    public static void PlaySoundWithoutOverlappingForBlock(SoundEffect sound, Vector2 position, float delayAfterSound = 0.5f)
    {
        StopSoundsForBlock(position);
        var volume = AudioManager.Instance.SoundsVolume * AudioManager.Instance.MasterVolume;
        var soundInstance = sound.CreateInstance();
        soundInstance.Volume = volume;
        soundInstance.Play();
        
        ActiveBlockSounds[position] = soundInstance;
        
        // delay
        var currentTime = (float) BlastiaGame.GameTime.TotalGameTime.TotalSeconds;
        var soundDuration = (float) sound.Duration.TotalSeconds;
        var delayEndTime = currentTime + soundDuration + delayAfterSound;
        BlockSoundDelays[position] = new DelayInfo(delayEndTime, delayAfterSound);
    }

    /// <summary>
    /// Use in pair with <see cref="PlaySoundWithoutOverlappingForBlock"/>. Stops active sounds for this block position
    /// </summary>
    /// <param name="position"></param>
    public static void StopSoundsForBlock(Vector2 position)
    {
        if (ActiveBlockSounds.TryGetValue(position, out var sound))
        {
            sound.Stop();
            sound.Dispose();
            ActiveBlockSounds.Remove(position);
            BlockSoundDelays.Remove(position);
        }
    }
    
    /// <summary>
    /// Use in pair with <see cref="PlaySoundWithoutOverlappingForBlock"/>. Checks if sound can be played (delay passed and no sound is playing)
    /// for this block position
    /// </summary>
    /// <param name="position"></param>
    public static bool CanPlaySoundForBlock(Vector2 position)
    {
        return !IsSoundPlayingForBlock(position) && !IsSoundInDelayForBlock(position);
    }

    private static bool IsSoundPlayingForBlock(Vector2 position)
    {
        if (ActiveBlockSounds.TryGetValue(position, out var sound))
        {
            return sound.State == SoundState.Playing;
        }

        return false;
    }

    private static bool IsSoundInDelayForBlock(Vector2 position)
    {
        var currentTime = (float) BlastiaGame.GameTime.TotalGameTime.TotalSeconds;
        if (BlockSoundDelays.TryGetValue(position, out var delay))
        {
            if (currentTime < delay.DelayEndTime)
            {
                return true;
            }
        }

        return false;
    }
    
    #endregion
    
    #region Procedural Sounds
    public static void PlayProceduralBlockBreakSound(Vector2 position, BlockMaterial material, float hardness, float delayAfterSound = 0.5f) 
    {
        var cacheKey = $"break_{material}_{hardness:F1}";
        SoundEffect? sound;
        
        if (!_proceduralSoundCache.TryGetValue(cacheKey, out sound)) 
        {
            sound = _soundGenerator.GenerateBlockBreakSound(material, hardness);
            CacheProceduralSound(cacheKey, sound);
        }

        PlaySoundWithoutOverlappingForBlock(sound, position, delayAfterSound);
    }
    
    private static void CacheProceduralSound(string cacheKey, SoundEffect sound) 
    {
        // limit cache size
        if (_proceduralSoundCache.Count >= MaxCacheSize) 
        {
            // remove oldest entry
            var firstKey = _proceduralSoundCache.Keys.First();
            _proceduralSoundCache[firstKey].Dispose();
            _proceduralSoundCache.Remove(firstKey);
        }

        _proceduralSoundCache[cacheKey] = sound;
    }
    
    #endregion

    /// <summary>
    /// Cleans up finished sounds
    /// </summary>
    public static void Update()
    {
        var currentTime = (float) BlastiaGame.GameTime.TotalGameTime.TotalSeconds;
        
        // clean up
        var finishedBlockSoundPositions = new List<Vector2>();
        foreach (var kvp in ActiveBlockSounds)
        {
            if (kvp.Value.State == SoundState.Stopped)
            {
                finishedBlockSoundPositions.Add(kvp.Key);
            }
        }

        foreach (var position in finishedBlockSoundPositions)
        {
            ActiveBlockSounds[position].Dispose();
            ActiveBlockSounds.Remove(position);
        }
        
        // clear delays
        var expiredBlockDelayPositions = new List<Vector2>();
        foreach (var kvp in BlockSoundDelays)
        {
            if (currentTime >= kvp.Value.DelayEndTime)
            {
                expiredBlockDelayPositions.Add(kvp.Key);
            }
        }

        foreach (var position in expiredBlockDelayPositions)
        {
            BlockSoundDelays.Remove(position);
        }
    }

    public static void UnloadSounds()
    {
        foreach (var sound in _sounds.Values)
        {
            sound.Dispose();
        }
        _sounds.Clear();
        
        foreach (var proceduralSound in _proceduralSoundCache.Values) 
        {
            proceduralSound.Dispose();
        }
        _proceduralSoundCache.Clear();
    }
}