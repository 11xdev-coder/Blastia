using Blastia.Main.Blocks.Common;

namespace Blastia.Main.GameState;

public static class WorldGen
{
    private static readonly NoisePass DirtNoisePass = new()
    {
        Frequency = 0.05f, 
        Octaves = 4, 
        Persistence = 0.5f, 
        Threshold = 0.45f,
        Amplitude = 1.0f,
        Block = BlockID.Dirt
    };
    
    public static void Generate(uint seed, WorldState worldState)
    {
        int width = worldState.WorldWidth;
        int height = worldState.WorldHeight;

        var noisePasses = new List<NoisePass>
        {
            DirtNoisePass
        };
		
        for (int x = 0; x < width; x++) 
        {
            for (int y = 0; y < height; y++)
            {
                foreach (var pass in noisePasses)
                {
                    float noiseValue = Noise.OctavePerlin(x, y, 
                        pass.Frequency, pass.Octaves, pass.Persistence) * pass.Amplitude;

                    if (noiseValue > pass.Threshold)
                    {
                        worldState.SetTile(x, y, pass.Block);
                    }
                }
            }
        }
    }
}