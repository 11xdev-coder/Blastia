using Blastia.Main.Utilities;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Blastia.Main.Sounds;

public enum MusicID
{
    PeacefulJourney00,
    PeacefulJourney01,
    PeacefulJourney02,
    PeacefulJourney03
}

public static class MusicEngine
{
    private static readonly Dictionary<MusicID, Song> MusicTracks = new();
    
    private static ContentManager? _contentManager;
    private static MusicID? _currentlyPlaying;

    private static float _fadeDuration = 1f;

    public static void Initialize(ContentManager contentManager)
    {
        _contentManager = contentManager;
    }

    public static void LoadMusic()
    {
        LoadMusicTrack(MusicID.PeacefulJourney00, Paths.PeacefulJourney00);
        LoadMusicTrack(MusicID.PeacefulJourney01, Paths.PeacefulJourney01);
        LoadMusicTrack(MusicID.PeacefulJourney02, Paths.PeacefulJourney02);
        LoadMusicTrack(MusicID.PeacefulJourney03, Paths.PeacefulJourney03);
    }

    private static void LoadMusicTrack(MusicID musicId, string path)
    {
        if (_contentManager == null) return;

        try
        {
            MusicTracks[musicId] = _contentManager.Load<Song>(path);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteLine($"Error while loading track. ID: {musicId}, Exception: {ex.Message}");
        }
    }

    /// Plays the specified music track, optionally looping it. If another track is currently playing, it will be faded out before the new track starts.
    /// <param name="musicId">The ID of the music track to play.</param>
    /// <param name="loop">Determines whether the music track should loop when it reaches the end. Defaults to true.</param>
    /// <returns>A Task representing the asynchronous music play operation.</returns>
    public static async void PlayMusicTrack(MusicID musicId, bool loop = true)
    {
        await FadeOutMusicTrack();
        
        // get the Song (music track)
        if (MusicTracks.TryGetValue(musicId, out var musicTrack))
        {
            MediaPlayer.IsRepeating = loop;
            UpdateVolume();
            MediaPlayer.Play(musicTrack);
            
            _currentlyPlaying = musicId;
            await FadeInMusicTrack();
            
            ConsoleHelper.WriteLine($"Music volume: {CalculateVolume()}");
        }
        else
        {
            ConsoleHelper.WriteLine($"Music track {musicId} not found.");
        }
    }

    /// Fades out the currently playing music track over the specified fade duration.
    /// The music volume will gradually decrease to zero before stopping the track.
    /// <returns>A Task representing the asynchronous fade-out operation.</returns>
    private static async Task FadeOutMusicTrack()
    {
        if (MediaPlayer.State == MediaState.Playing)
        {
            var initialVolume = MediaPlayer.Volume;
            for (float t = 0; t < _fadeDuration; t += 0.1f)
            {
                MediaPlayer.Volume = initialVolume * (1 - t / _fadeDuration);
                await Task.Delay(100);
            }

            MediaPlayer.Volume = 0;
            MediaPlayer.Stop();
        }
    }

    /// Gradually increases the volume of the currently playing music track over a specified fade duration.
    /// The volume will increase from zero to the calculated music volume.
    /// <returns>A Task representing the asynchronous fade-in operation.</returns>
    private static async Task FadeInMusicTrack()
    {
        var finalVolume = CalculateVolume();
        for (float t = 0; t < _fadeDuration; t += 0.1f)
        {
            MediaPlayer.Volume = finalVolume * (t / _fadeDuration);
            await Task.Delay(100);
        }
        
        MediaPlayer.Volume = finalVolume;
    }

    public static void StopMusic()
    {
        MediaPlayer.Stop();
    }

    public static void UpdateVolume()
    {
        MediaPlayer.Volume = CalculateVolume();
    }

    private static float CalculateVolume()
    {
        return AudioManager.Instance.MusicVolume 
               * AudioManager.Instance.MasterVolume;
    }

    public static void UnloadMusic()
    {
        StopMusic();
        MusicTracks.Clear();
    }
}