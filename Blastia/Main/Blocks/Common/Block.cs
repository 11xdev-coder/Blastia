using Blastia.Main.Entities;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks.Common;

/// <summary>
/// Base block class
/// </summary>
[Serializable]
public abstract class Block 
{
	public static readonly int Size = 8;
	public static readonly float AirDragCoefficient = 50f;
	
	public ushort Id { get; }
	public string Name { get; } = string.Empty;
	/// <summary>
	/// How much drag to apply when entity walks on this block. 0 = no drag force
	/// </summary>
	public float DragCoefficient { get; } = 50f;
	public float Hardness { get; } = 1f;
	public bool IsCollidable { get; } = true;
	public bool IsTransparent { get; }
	public ushort ItemIdDrop { get; }
	public int ItemDropAmount { get; } = 1;
	public int LightLevel { get; }

	protected Block()
	{
		
	}

	protected Block(ushort id, string name, float dragCoefficient = 50f, float hardness = 1f,
		bool isCollidable = true, bool isTransparent = false, ushort itemIdDrop = 0, int itemDropAmount = 1, int lightLevel = 0)
	{
		Id = id;
		Name = name;
		DragCoefficient = dragCoefficient;
		Hardness = hardness;
		IsCollidable = isCollidable;
		IsTransparent = isTransparent;
		ItemIdDrop = itemIdDrop;
		ItemDropAmount = itemDropAmount;
		LightLevel = lightLevel;
	}

	// virtual methods for complex blocks
	public virtual void OnPlace(World world, Vector2 position, Player player) {}

	public virtual void OnBreak(World? world, Vector2 position, Player? player)
	{
		// TODO: Randomize speed, tweak position and scale
		if (world == null) return;
		
		var droppedItem = new DroppedItem(position, 1f, world);
		var item = StuffRegistry.GetItem(ItemIdDrop);
		droppedItem.Launch(item, ItemDropAmount, 1, 10f, 15f);
		BlastiaGame.RequestAddEntity(droppedItem);
	}
	public virtual void OnRightClick(World world, Vector2 position, Player player) {}
	public virtual void OnLeftClick(World world, Vector2 position, Player player) {}
	public virtual void Update(World world, Vector2 position) {}
	public virtual void OnNeighbourChanged(World world, Vector2 position, Vector2 neighbourPosition) {}
	
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
		var texture = StuffRegistry.GetBlockTexture(Id);
		spriteBatch.Draw(texture, destRectangle, sourceRectangle, Color.White);
	}
}