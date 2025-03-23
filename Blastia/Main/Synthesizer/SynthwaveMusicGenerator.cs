namespace Blastia.Main.Synthesizer;

public class SynthwaveMusicGenerator
{
    /// <summary>
    /// Helper to compute 16th-note step duration in seconds
    /// </summary>
    /// <param name="tempo"></param>
    /// <returns></returns>
    private static double StepDuration(float tempo)
    {
        return (60.0 / tempo) / 4;
    }
    
    #region Into The Abyss
    
    /// <summary>
    /// Section 1: Bass beat repeating every 3.5s with 2 identical hits separated by 0.5s gap
    /// </summary>
    /// <param name="track"></param>
    /// <param name="tempo"></param>
    /// <param name="sectionStartSec"></param>
    /// <param name="sectionEndSec"></param>
    /// <returns></returns>
    public static TrackPart IntoTheAbyssBassBeat(MusicTrack track, float tempo, double sectionStartSec, double sectionEndSec)
    {
        TrackPart bassPart = new TrackPart
        {
            Type = TrackPartType.Bass,
            Channel = 0,
            Program = 38 // synth bass 1
        };

        double stepDur = StepDuration(tempo);
        int sectionStartStep = (int)(sectionStartSec / stepDur);
        int sectionEndStep = (int)(sectionEndSec / stepDur);

        double cycleSec = 3.5;
        int cycleSteps = (int) (cycleSec / stepDur);
        double gapSec = 0.5;
        int gapSteps = (int) (gapSec / stepDur);
        double beatDurSec = 0.2;
        int beatDurSteps = (int) (beatDurSec / stepDur);

        int bassNote = 36 + track.Key;

        for (int cycleStart = sectionStartStep; cycleStart < sectionEndStep; cycleStart += cycleSteps)
        {
            // first beat at cycle start
            MusicNote note1 = new MusicNote
            {
                Note = bassNote,
                Velocity = 100,
                StartStep = cycleStart,
                Duration = beatDurSteps,
                Channel = bassPart.Channel
            };
            bassPart.Notes.Add(note1);
            
            int secondBeatStep = cycleStart + gapSteps;
            if (secondBeatStep < sectionEndStep)
            {
                MusicNote note2 = new MusicNote
                {
                    Note = bassNote,
                    Velocity = 100,
                    StartStep = secondBeatStep,
                    Duration = beatDurSteps,
                    Channel = bassPart.Channel
                };
                bassPart.Notes.Add(note2);
            }
        }

        return bassPart;
    }
    
    #endregion
}