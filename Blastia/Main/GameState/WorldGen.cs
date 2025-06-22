using Blastia.Main.Blocks.Common;

namespace Blastia.Main.GameState;

public static class WorldGen
{
    private static readonly NoisePass DirtNoisePass = new()
    {
        Frequency = 0.1f, 
        Octaves = 2, 
        Persistence = 0.15f, 
        Threshold = 0.3f,
        Amplitude = 1f,
        HeightScale = 0.1f,
        MaxHeight = 0.4f,
        Block = BlockId.Dirt
    };
    
    public static void Generate(uint seed, WorldState worldState)
    {
        int width = worldState.WorldWidth;
        int height = worldState.WorldHeight;

        var noisePasses = new List<NoisePass>
        {
            DirtNoisePass
        };

        int halfWorld = (int) (width * 0.5f);
        int randomSpawnX = BlastiaGame.Rand.Next(halfWorld - 20, halfWorld + 21);
        bool spawnPointSet = false;
        
        for (int x = 0; x < width; x++) 
        {
            foreach (var pass in noisePasses)
            {
                float noiseValue = Noise.OctavePerlin(x, 0, 
                    pass.Frequency, pass.Octaves, pass.Persistence) * pass.Amplitude;
                
                // Calculate base height as percentage of world height
                int baseHeight = (int)(height * pass.MaxHeight);
                
                // Apply variation around base height
                int terrainVariation = (int)(height * pass.HeightScale * noiseValue);
                int finalHeight = baseHeight + terrainVariation;
                
                int alignedHeight = (finalHeight / Block.Size) * Block.Size;
                for (int y = alignedHeight; y < height; y += Block.Size)
                {
                    worldState.SetTile(x * Block.Size, y, BlockId.Dirt, TileLayer.Ground);
                }
                
                if (x >= randomSpawnX)
                {
                    if (spawnPointSet) return;
                    
                    // five blocks above current height
                    worldState.SetSpawnPoint(x, finalHeight - Block.Size * 3);
                    spawnPointSet = true;
                }
            }
        }
    }
}