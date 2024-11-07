using Blastia.Main.Blocks.Common;

namespace Blastia.Main.GameState;

public static class WorldGen
{
    public static void Generate(uint seed, WorldState worldState)
    {
        int width = worldState.WorldWidth;
        int height = worldState.WorldHeight;
		
        for (int x = 0; x < width; x++) 
        {
            for (int y = 0; y < height; y++) 
            {
                worldState.SetTile(x, y, BlockID.Stone);
            }
        }
    }
}