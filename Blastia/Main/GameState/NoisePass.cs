namespace Blastia.Main.GameState;

public class NoisePass
{
    // lower -> larger
    public float Frequency { get; set; }
    // more -> more fractal-like pattern
    public int Octaves { get; set; }
    // rate each octave decreases
    public float Persistence { get; set; }
    public float Threshold { get; set; }
    // strength/intensity
    public float Amplitude { get; set; }
    public float HeightScale { get; set; }
    public float MaxHeight { get; set; }
    public ushort Block { get; set; }
}