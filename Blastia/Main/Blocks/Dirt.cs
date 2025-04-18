using Blastia.Main.Blocks.Common;

namespace Blastia.Main.Blocks;

public class Dirt : Block 
{
	public override ushort ID => BlockID.Dirt;

	public override float DragCoefficient => 12f;
}