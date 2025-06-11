using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks.Common;

public static class BlockRectangles 
{
	// 2 pixel offset from each tile
	public static readonly int Offset = 2; 
	
	public static readonly Rectangle TopLeft = new(0, 0, Block.Size, Block.Size);
	public static readonly Rectangle Top = new(Block.Size + Offset, 0, Block.Size, Block.Size);
	public static readonly Rectangle TopRight = new((Block.Size + Offset) * 2, 0, Block.Size, Block.Size);
	public static readonly Rectangle Left = new(0, Block.Size + Offset, Block.Size, Block.Size);
	public static readonly Rectangle Middle = new(Block.Size + Offset, Block.Size + Offset, Block.Size, Block.Size);
	public static readonly Rectangle Right = new((Block.Size + Offset) * 2, Block.Size + Offset, Block.Size, Block.Size);
	public static readonly Rectangle BottomLeft = new(0, (Block.Size + Offset) * 2, Block.Size, Block.Size);
	public static readonly Rectangle Bottom = new(Block.Size + Offset, (Block.Size + Offset) * 2, Block.Size, Block.Size);
	public static readonly Rectangle BottomRight = new((Block.Size + Offset) * 2, (Block.Size + Offset) * 2, Block.Size, Block.Size);
	public static readonly Rectangle TopBottom = new((Block.Size + Offset) * 3, 0, Block.Size, Block.Size);
	public static readonly Rectangle LeftTopRight = new((Block.Size + Offset) * 4, 0, Block.Size, Block.Size);
	public static readonly Rectangle BottomLeftTop = new((Block.Size + Offset) * 3, Block.Size + Offset, Block.Size, Block.Size);
	public static readonly Rectangle All = new((Block.Size + Offset) * 4, Block.Size + Offset, Block.Size, Block.Size);
	public static readonly Rectangle BottomRightTop = new((Block.Size + Offset) * 5, Block.Size + Offset, Block.Size, Block.Size);
	public static readonly Rectangle LeftRight = new((Block.Size + Offset) * 3, (Block.Size + Offset) * 2, Block.Size, Block.Size);
	public static readonly Rectangle LeftBottomRight = new((Block.Size + Offset) * 4, (Block.Size + Offset) * 2, Block.Size, Block.Size);
	
	public static readonly Rectangle DestroyOne = new(0, 0, Block.Size, Block.Size);
	public static readonly Rectangle DestroyTwo = new(Block.Size, 0, Block.Size, Block.Size);
	public static readonly Rectangle DestroyThree = new(Block.Size * 2, 0, Block.Size, Block.Size);
	public static readonly Rectangle DestroyFour = new(Block.Size * 3, 0, Block.Size, Block.Size);
	public static readonly Rectangle DestroyFive = new(Block.Size * 4, 0, Block.Size, Block.Size);
	public static readonly Rectangle DestroySix = new(Block.Size * 5, 0, Block.Size, Block.Size);
}