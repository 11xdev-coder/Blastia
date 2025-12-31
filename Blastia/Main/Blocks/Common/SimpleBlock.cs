using Blastia.Main.Sounds;

namespace Blastia.Main.Blocks.Common;

/// <summary>
/// Simple block, loaded from json
/// </summary>
public class SimpleBlock : Block
{
    public SimpleBlock()
    {
        
    }

    public SimpleBlock(ushort id, string name, float dragCoefficient = 50f, float hardness = 1f, bool isBreakable = true,
        bool isCollidable = true, bool isTransparent = false, ushort itemIdDrop = 0, int itemDropAmount = 1, int lightLevel = 0, 
        SoundID[]? breakingSounds = null)
        : base(id, name, dragCoefficient, hardness, isBreakable, isCollidable, isTransparent, itemIdDrop, itemDropAmount, lightLevel, breakingSounds)
    {
        
    }

    public override Block Clone()
    {
        return new SimpleBlock(Id, Name, DragCoefficient, Hardness, IsBreakable, IsCollidable, IsTransparent, ItemIdDrop, ItemDropAmount, LightLevel, BreakingSounds);
    }
}