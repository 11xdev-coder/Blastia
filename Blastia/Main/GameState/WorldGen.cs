using Blastia.Main.Blocks.Common;

namespace Blastia.Main.GameState;

public static class WorldGen
{
    private static readonly NoisePass DirtNoisePass = new()
    {
        Frequency = 0.03f, 
        Octaves = 4, 
        Persistence = 0.3f, 
        Threshold = 0.3f,
        Amplitude = 1.0f,
        HeightScale = 0.4f,
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
            foreach (var pass in noisePasses)
            {
                float noiseValue = Noise.OctavePerlin(x, 0, 
                    pass.Frequency, pass.Octaves, pass.Persistence) * pass.Amplitude;
                
                int terrainHeight = (int)(height * pass.HeightScale * noiseValue);
                
                for (int y = terrainHeight; y < height; y++)
                {
                    worldState.SetTile(x, y, pass.Block);
                }
            }
        }
    }
}