using Blastia.Main.Blocks.Common;

namespace Blastia.Main.Blocks;

public class Stone : Block 
{
    public override ushort ID => BlockID.Stone;
    public override float DragCoefficient => 15f;
}