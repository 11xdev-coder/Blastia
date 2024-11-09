using Blastia.Main.Blocks.Common;

namespace Blastia.Main.GameState;

public static class WorldGen
{
    private static readonly NoisePass DirtNoisePass = new()
    {
        Frequency = 0.01f, 
        Octaves = 2, 
        Persistence = 0.15f, 
        Threshold = 0.3f,
        Amplitude = 1f,
        HeightScale = 0.1f,
        MaxHeight = 100,
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

                float heightMultiplier = (float) pass.MaxHeight / height;
                int terrainVariation = (int)((height * pass.HeightScale * noiseValue) 
                    / heightMultiplier);
                
                for (int y = terrainVariation; y < height; y++)
                {
                    worldState.SetTile(x, y, pass.Block);
                }
            }
        }
    }
}