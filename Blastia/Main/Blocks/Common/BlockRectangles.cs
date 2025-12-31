using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks.Common;

public static class BlockRectangles 
{
	public static readonly int SpriteSize = 8;
	// 2 pixel offset from each tile
	public static readonly int Offset = 2; 
	
	public static readonly Rectangle TopLeft = new(0, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle Top = new(SpriteSize + Offset, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle TopRight = new((SpriteSize + Offset) * 2, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle Left = new(0, SpriteSize + Offset, SpriteSize, SpriteSize);
	public static readonly Rectangle Middle = new(SpriteSize + Offset, SpriteSize + Offset, SpriteSize, SpriteSize);
	public static readonly Rectangle Right = new((SpriteSize + Offset) * 2, SpriteSize + Offset, SpriteSize, SpriteSize);
	public static readonly Rectangle BottomLeft = new(0, (SpriteSize + Offset) * 2, SpriteSize, SpriteSize);
	public static readonly Rectangle Bottom = new(SpriteSize + Offset, (SpriteSize + Offset) * 2, SpriteSize, SpriteSize);
	public static readonly Rectangle BottomRight = new((SpriteSize + Offset) * 2, (SpriteSize + Offset) * 2, SpriteSize, SpriteSize);
	public static readonly Rectangle TopBottom = new((SpriteSize + Offset) * 3, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle LeftTopRight = new((SpriteSize + Offset) * 4, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle BottomLeftTop = new((SpriteSize + Offset) * 3, SpriteSize + Offset, SpriteSize, SpriteSize);
	public static readonly Rectangle All = new((SpriteSize + Offset) * 4, SpriteSize + Offset, SpriteSize, SpriteSize);
	public static readonly Rectangle BottomRightTop = new((SpriteSize + Offset) * 5, SpriteSize + Offset, SpriteSize, SpriteSize);
	public static readonly Rectangle LeftRight = new((SpriteSize + Offset) * 3, (SpriteSize + Offset) * 2, SpriteSize, SpriteSize);
	public static readonly Rectangle LeftBottomRight = new((SpriteSize + Offset) * 4, (SpriteSize + Offset) * 2, SpriteSize, SpriteSize);
	
	public static readonly Rectangle DestroyOne = new(0, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle DestroyTwo = new(SpriteSize, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle DestroyThree = new(SpriteSize * 2, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle DestroyFour = new(SpriteSize * 3, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle DestroyFive = new(SpriteSize * 4, 0, SpriteSize, SpriteSize);
	public static readonly Rectangle DestroySix = new(SpriteSize * 5, 0, SpriteSize, SpriteSize);
}