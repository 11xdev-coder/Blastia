using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace BlasterMaster.Main.Sounds;

public enum MusicID
{
    PeacefulJourney00,
    PeacefulJourney01,
    PeacefulJourney02,
    PeacefulJourney03
}

public static class MusicEngine
{
    private static Dictionary<MusicID, Song> _musicTracks = new();
    private static ContentManager? _contentManager;
    private static MusicID? _currentlyPlaying;

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
            _musicTracks[musicId] = _contentManager.Load<Song>(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while loading track. ID: {musicId}, Exception: {ex.Message}");
        }
    }

    public static void PlayMusicTrack(MusicID musicId, bool loop = true)
    {
        // get the Song (music track)
        if (_musicTracks.TryGetValue(musicId, out var musicTrack))
        {
            MediaPlayer.IsRepeating = loop;
            UpdateVolume();
            MediaPlayer.Play(musicTrack);
            
            _currentlyPlaying = musicId;
            
            Console.WriteLine($"Music volume: {GetVolume()}");
        }
        else
        {
            Console.WriteLine($"Music track {musicId} not found.");
        }
    }

    public static void StopMusic()
    {
        MediaPlayer.Stop();
    }

    public static void UpdateVolume()
    {
        MediaPlayer.Volume = GetVolume();
    }

    private static float GetVolume()
    {
        return AudioManager.Instance.MusicVolume 
               * AudioManager.Instance.MasterVolume;
    }

    public static void UnloadMusic()
    {
        StopMusic();
        _musicTracks.Clear();
    }
}