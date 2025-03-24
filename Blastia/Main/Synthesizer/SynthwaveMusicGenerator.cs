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
            Program = 39 // synth bass 1
        };

        // create synth oscillator
        SynthParameters synthParams = new SynthParameters();
        
        WaveParameters oscillator = new WaveParameters
        {
            WaveType = WaveType.Sawtooth,
            Amplitude = 0.6f,
            IsEnabled = true,
            Envelope = new EnvelopeParameters
            {
                AttackTime = 0.1f,
                DecayTime = 1.5f,
                SustainLevel = 1f,
                ReleaseTime = 1.5f
            }
        };
        WaveParameters oscillator2 = new WaveParameters
        {
            WaveType = WaveType.Triangle,
            Amplitude = 0.6f,
            IsEnabled = true,
            Envelope = new EnvelopeParameters
            {
                AttackTime = 0.1f,
                DecayTime = 1.5f,
                SustainLevel = 1f,
                ReleaseTime = 1.5f
            }
        };
        synthParams.Oscillators.AddRange([oscillator, oscillator2]);
        bassPart.SynthParams = synthParams;
        
        double stepDur = StepDuration(tempo);
        int sectionStartStep = (int)(sectionStartSec / stepDur);
        int sectionEndStep = (int)(sectionEndSec / stepDur);

        double cycleSec = 3.5;
        int cycleSteps = (int) (cycleSec / stepDur);
        double gapSec = 0.6;
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

    public static TrackPart IntoTheAbyssSmallArpeggio(MusicTrack track, float tempo, double sectionStartSec,
        double sectionEndSec, double eventTimeStartSec)
    {
        TrackPart arpPart = new TrackPart
        {
            Type = TrackPartType.Arpeggio,
            Channel = 1,
            Program = 81
        };
        
        SynthParameters synthParams = new SynthParameters();

        var envelope = new EnvelopeParameters
        {
            AttackTime = 1.5f,
            DecayTime = 1.5f,
            SustainLevel = 1f,
            ReleaseTime = 3f
        };
        WaveParameters oscillator = new WaveParameters
        {
            WaveType = WaveType.Sine,
            Amplitude = 0.9f,
            IsEnabled = true,
            Envelope = envelope
        };
        WaveParameters oscillator2 = new WaveParameters
        {
            WaveType = WaveType.Triangle,
            Amplitude = 0.7f,
            IsEnabled = true,
            Envelope = envelope
        };
        WaveParameters oscillator3 = new WaveParameters
        {
            WaveType = WaveType.Sawtooth,
            Amplitude = 0.5f,
            IsEnabled = true,
            Envelope = envelope
        };
        synthParams.Oscillators.AddRange([oscillator, oscillator2, oscillator3]);
        arpPart.SynthParams = synthParams;

        double stepDur = StepDuration(tempo);
        int sectionStartStep = (int) (sectionStartSec / stepDur);
        int sectionEndStep = (int) (sectionEndSec / stepDur);
        Random rng = new Random();
        
        int pairGap = (int) (1.4 / stepDur);
        int pairGapSteps = (int) (0.6 / stepDur);
        int eventIntervalSteps = (int)(8 / stepDur);
        int currentStep = sectionStartStep;
        int pairCount = 1;
        
        while (currentStep < sectionEndStep)
        {
            int[] scale = [0, 2, 4, 5, 7, 9, 11];
            int baseNote = 60; // c4

            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                int scaleDegree = scale[rng.Next(scale.Length)];
                int pitch = baseNote + scaleDegree;

                if (rng.NextDouble() < 0.3)
                {
                    pitch += rng.Next(-1, 8);
                }
                
                // 3-step spacing between pairs within same event
                int pairStartStep = currentStep + pairIndex * pairGap;
                
                // first note
                MusicNote note1 = new MusicNote
                {
                    Note = pitch,
                    Velocity = 70,
                    StartStep = pairStartStep,
                    Duration = 1, // Short note
                    Channel = arpPart.Channel
                };
                arpPart.Notes.Add(note1);

                // last note of last pair is long
                int lastDuration = 1;
                if (pairIndex == pairCount - 1)
                {
                    lastDuration = 7;
                }
                // second note
                MusicNote note2 = new MusicNote
                {
                    Note = pitch,
                    Velocity = 70,
                    StartStep = pairStartStep + pairGapSteps,
                    Duration = lastDuration,
                    Channel = arpPart.Channel
                };
                arpPart.Notes.Add(note2);
            }

            currentStep += eventIntervalSteps;
            double eventTimeSec = currentStep * stepDur;
            if (eventTimeSec > eventTimeStartSec)
            {
                pairCount = 3;
            }
        }
        
        return arpPart;
    }
    
    #endregion
}