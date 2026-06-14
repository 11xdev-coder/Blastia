using System.Numerics;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Persistence;
using Vector2 = Microsoft.Xna.Framework.Vector2;

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
        
        SurfaceHeights = new int[state.WorldWidth];
    }
    
    public void SetTile(int x, int y, ushort id, TileLayer layer) => WorldState.SetTile(x, y, id, layer);
    public void SetSpawn(int x, int y) => WorldState.Spawn = new Vector2(x, y);
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
    /// Fraction of the world where terrain starts. 0.4f - world starts from 40% of height (from 40% to bottom)
    /// </summary>
    public float MaxHeight { get; }
    /// <summary>
    /// How dramatic terrain variation is
    /// </summary>
    public float HeightScale { get; }
    public ushort BaseBlock { get; }
}

public class DirtPass : ITerrainGenPass 
{
    public string Name => "Placing dirt";
    public float Frequency => 0.03f;
    //public int Octaves => 2;
    //public float Persistence => 0.15f;
    //public float Threshold => 0.3f;
    public float HeightScale => 0.1f;
    public float MaxHeight => 0.4f;
    public ushort BaseBlock => BlockId.Dirt;
    
    public void Run(WorldGenContext context) 
    {
        for (int x = 0; x < context.WorldWidth; x++) 
        {
            float noiseValue = Noise.Perlin(x * Frequency, 0);
            
            int baseHeight = (int) (context.WorldHeight * MaxHeight);
            int variation = (int) (context.WorldHeight * HeightScale * noiseValue);
            int height = (baseHeight + variation) / Block.Size * Block.Size;
            
            for (int y = height; y < context.WorldHeight; y += Block.Size) 
            {
                context.SetTile(x, y, BaseBlock, TileLayer.Ground);
            }
            context.SurfaceHeights[x] = height;
        }
    }
}

public class SpawnPointPass : IWorldGenPass 
{
    public string Name => "Setting spawn point";
    
    public void Run(WorldGenContext context) 
    {
        int halfWorld = (int) (context.WorldWidth * 0.5f);
        int randomSpawnX = BlastiaGame.Rand.Next(halfWorld - 20, halfWorld + 21);
        
        for (int x = 0; x < context.WorldWidth; x++) 
        {
            if (x >= randomSpawnX) 
            {
                context.SetSpawn(x, context.SurfaceHeights[x]);
                return;
            }
        }
    }
}

public static class WorldGen
{
    public volatile static float Progress = 0f;
    
    private static readonly List<IWorldGenPass> Passes = [
        new DirtPass(),
        new SpawnPointPass()
    ];
    
    public static void Generate(BigInteger seed, WorldState worldState)
    {
        var context = new WorldGenContext(worldState, seed);
        for (int i = 0; i < Passes.Count; i++) 
        {
            Progress = (float) i / Passes.Count;
            Passes[i].Run(context);
        }
        
        Progress = 1f;
    }
}