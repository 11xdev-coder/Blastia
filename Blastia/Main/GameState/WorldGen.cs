using System.Numerics;
using Blastia.Main.Blocks.Common;

namespace Blastia.Main.GameState;

public class WorldGenContext 
{
    public WorldState WorldState { get; }
    public BigInteger Seed { get; }
    public int WorldWidth => WorldState.WorldWidth;
    public int WorldHeight => WorldState.WorldHeight;
    
    // surface heights for other passes
    public int[] SurfaceHeights { get; }
    
    public WorldGenContext(WorldState state, BigInteger seed) 
    {
        WorldState = state;
        Seed = seed;
        
        SurfaceHeights = [];
    }
}

public interface IWorldGenPass 
{
    public string Name { get; }
    public void Run(WorldGenContext context);
}

public interface ITerrainGenPass : IWorldGenPass
{
    /// <summary>
    /// Coordinate (x and y) multiplier
    /// </summary>
    public float Frequency { get; }
    /// <summary>
    /// Noise value multiplier
    /// </summary>
    public float Amplitude { get; }
}

public class DirtPass : ITerrainGenPass 
{
    public string Name => "Placing dirt";
    public float Frequency => 0.1f;
    public int Octaves => 2;
    public float Persistence => 0.15f;
    public float Threshold => 0.3f;
    public float Amplitude => 1f;
    public float HeightScale => 0.1f;
    public float MaxHeight => 0.4f;
    public ushort BaseBlock => BlockId.Dirt;
    
    public void Run(WorldGenContext context) 
    {
        for (int x = 0; x < context.WorldWidth; x++) 
        {
            float noiseValue = Noise.Perlin(x * Frequency, 0) * Amplitude;
            
            for (int y = (int) noiseValue; y < context.WorldHeight; y += Block.Size) 
            {
                context.WorldState.SetTile(x * Block.Size, y, BaseBlock, TileLayer.Ground);
            }
            
            // Calculate base height as percentage of world height
            //int baseHeight = (int)(context.WorldHeight * MaxHeight);
            
            // Apply variation around base height
            //int terrainVariation = (int)(context.WorldHeight * HeightScale * noiseValue);
            //int finalHeight = baseHeight + terrainVariation;
            
            //int alignedHeight = finalHeight / Block.Size * Block.Size;
            //for (int y = alignedHeight; y < context.WorldHeight; y += Block.Size)
            //{
            //    context.WorldState.SetTile(x * Block.Size, y, BaseBlock, TileLayer.Ground);
            //}
            
            //if (x >= randomSpawnX)
            //{
            //    if (spawnPointSet) continue;
                
                // five blocks above current height
            //    worldState.SetSpawnPoint(x, finalHeight - Block.Size * 3);
            //    spawnPointSet = true;
            //}
        }
    }
}

public static class WorldGen
{
    public volatile static float Progress = 0f;
    
    private static readonly List<IWorldGenPass> Passes = [
        new DirtPass()
    ];
    
    public static void Generate(BigInteger seed, WorldState worldState)
    {
        //int halfWorld = (int) (width * 0.5f);
        //int randomSpawnX = BlastiaGame.Rand.Next(halfWorld - 20, halfWorld + 21);
        //bool spawnPointSet = false;
        
        var context = new WorldGenContext(worldState, seed);
        for (int i = 0; i < Passes.Count; i++) 
        {
            Progress = i / Passes.Count;
            Passes[i].Run(context);
        }
        
        Progress = 1f;
    }
}