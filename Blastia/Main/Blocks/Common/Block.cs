using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks.Common;

[Serializable]
public abstract class Block 
{
	public static readonly int Size = 8;
	public static readonly float AirDragCoefficient = 50f;
	
	public abstract ushort ID { get; }
	/// <summary>
	/// How much drag to apply when entity walks on this block. 0 = no drag force
	/// </summary>
	public abstract float DragCoefficient { get; }

	/// <summary>
	/// Returns source rectangle for drawing depending on neighbouring blocks
	/// </summary>
	/// <param name="emptyTop">Is block above air</param>
	/// <param name="emptyBottom">Is block below air</param>
	/// <param name="emptyRight">Is right block air</param>
	/// <param name="emptyLeft">Is left block air</param>
	/// <returns></returns>
	public Rectangle GetRuleTileSourceRectangle(bool emptyTop, bool emptyBottom, bool emptyRight, bool emptyLeft)
	{
		// 4 exposed sides
		if (emptyTop && emptyBottom && emptyLeft && emptyRight) return BlockRectangles.All; // Isolated block

		// 3
		if (emptyTop && emptyBottom && emptyLeft && !emptyRight) return BlockRectangles.BottomLeftTop;   // t b l
		if (emptyTop && emptyBottom && !emptyLeft && emptyRight) return BlockRectangles.BottomRightTop;  // t b r
		if (emptyTop && !emptyBottom && emptyLeft && emptyRight) return BlockRectangles.LeftTopRight;    // t l r
		if (!emptyTop && emptyBottom && emptyLeft && emptyRight) return BlockRectangles.LeftBottomRight; // b l r
		
		// 2
		// corners
		if (emptyTop && !emptyBottom && emptyLeft && !emptyRight) return BlockRectangles.TopLeft;     // t l
		if (emptyTop && !emptyBottom && !emptyLeft && emptyRight) return BlockRectangles.TopRight;    // t r
		if (!emptyTop && emptyBottom && emptyLeft && !emptyRight) return BlockRectangles.BottomLeft;  // b l
		if (!emptyTop && emptyBottom && !emptyLeft && emptyRight) return BlockRectangles.BottomRight; // b r
		// opposite
		if (emptyTop && emptyBottom && !emptyLeft && !emptyRight) return BlockRectangles.TopBottom;   // t b
		if (!emptyTop && !emptyBottom && emptyLeft && emptyRight) return BlockRectangles.LeftRight;   // l r

		// 1
		if (emptyTop && !emptyBottom && !emptyLeft && !emptyRight) return BlockRectangles.Top;        // t
		if (!emptyTop && emptyBottom && !emptyLeft && !emptyRight) return BlockRectangles.Bottom;     // b
		if (!emptyTop && !emptyBottom && emptyLeft && !emptyRight) return BlockRectangles.Left;       // l
		if (!emptyTop && !emptyBottom && !emptyLeft && emptyRight) return BlockRectangles.Right;      // r

		// 0
		return BlockRectangles.Middle;
	}
	
	public virtual void Draw(SpriteBatch spriteBatch, Rectangle destRectangle, Rectangle sourceRectangle)
	{
		var texture = StuffRegistry.GetBlockTexture(ID);
		spriteBatch.Draw(texture, destRectangle, sourceRectangle, Color.White);
	}
}