namespace Blastia.Main.Blocks.Common;

/// <summary>
/// Simple block, loaded from json
/// </summary>
public class SimpleBlock : Block
{
    public SimpleBlock()
    {
        
    }

    public SimpleBlock(ushort id, string name, float dragCoefficient = 50f, float hardness = 1f,
        bool isCollidable = true, bool isTransparent = false, ushort itemIdDrop = 0, int itemDropAmount = 1, int lightLevel = 0)
        : base(id, name, dragCoefficient, hardness, isCollidable, isTransparent, itemIdDrop, itemDropAmount, lightLevel)
    {
        
    }
}