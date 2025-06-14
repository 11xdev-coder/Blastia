using Blastia.Main.Entities;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Sounds;
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
	/// <summary>
	/// Time to break this block in seconds
	/// </summary>
	public float Hardness { get; } = 1f;
	public bool IsCollidable { get; } = true;
	public bool IsTransparent { get; }
	public ushort ItemIdDrop { get; }
	public int ItemDropAmount { get; } = 1;
	public int LightLevel { get; }
	public SoundID[]? BreakingSounds { get; }

	protected Block()
	{
		
	}

	protected Block(ushort id, string name, float dragCoefficient = 50f, float hardness = 1f,
		bool isCollidable = true, bool isTransparent = false, ushort itemIdDrop = 0, int itemDropAmount = 1, int lightLevel = 0,
		SoundID[]? breakingSounds = null)
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
		BreakingSounds = breakingSounds ?? [SoundID.Dig1, SoundID.Dig2, SoundID.Dig3];
	}

	// virtual methods for complex blocks
	public virtual void OnPlace(World world, Vector2 position, Player player) {}

	public virtual void OnBreak(World? world, Vector2 position, Player? player)
	{
		if (world == null) return;

		var rand = new Random();
		var randomDirection = rand.Next(2) == 0 ? -1 : 1;
		
		var correctPosition = new Vector2(position.X + Size * 0.5f, position.Y + Size * 0.5f);
		var droppedItem = new DroppedItem(correctPosition, 0.25f, world);
		var item = StuffRegistry.GetItem(ItemIdDrop);
		droppedItem.Launch(item, ItemDropAmount, randomDirection, 10f, 15f, 0f);
		BlastiaGame.RequestAddEntity(droppedItem);
	}

	public virtual float GetBreakTime() => Math.Max(0.05f, Hardness);
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

/// <summary>
/// Actual instance of a <c>Block</c> class with specific properties
/// </summary>
public class BlockInstance
{
	public Block Block;
	public float Damage;
	private readonly BlockBreakingAnimation _breakingAnimation;

	public BlockInstance(Block block, float damage)
	{
		Block = block;
		Damage = damage;
		_breakingAnimation = new BlockBreakingAnimation();
	}

	public ushort Id => Block.Id;

	/// <summary>
	/// Increments <c>Damage</c> property and breaks the block
	/// </summary>
	/// <param name="position"></param>
	/// <param name="player"></param>
	public void DoBreaking(Vector2 position, Player? player)
	{
		var selectedWorld = PlayerManager.Instance.SelectedWorld;
		if (selectedWorld == null) return;
		
		var deltaTime = (float) BlastiaGame.GameTime.ElapsedGameTime.TotalSeconds;
		Damage += deltaTime;

		if (!SoundEngine.IsSoundPlayingForBlock(position))
		{
			SoundEngine.PlaySoundWithoutOverlappingForBlock(ChooseRandomBreakingSound(), position);
		}
		
		if (!_breakingAnimation.IsAnimating)
		{
			_breakingAnimation.StartAnimation();
		}
		
		if (Damage >= Block.Hardness)
		{
			selectedWorld.SetTile((int) position.X, (int) position.Y, 0, player);
		}
	}

	private SoundID ChooseRandomBreakingSound()
	{
		if (Block.BreakingSounds == null) return SoundID.Dig1;
		var randomIndex = BlastiaGame.Rand.Next(0, Block.BreakingSounds.Length);
		return Block.BreakingSounds[randomIndex];
	}
	
	public void OnPlace(World world, Vector2 position, Player player) => Block.OnPlace(world, position, player);
	public void OnBreak(World? world, Vector2 position, Player? player) => Block.OnBreak(world, position, player);
	public float GetBreakTime() => Block.GetBreakTime();
	public void OnRightClick(World world, Vector2 position, Player player) => Block.OnRightClick(world, position, player);
	public void OnLeftClick(World world, Vector2 position, Player player) => Block.OnLeftClick(world, position, player);
	public void Update(World world, Vector2 position) => Block.Update(world, position);
	public void OnNeighbourChanged(World world, Vector2 position, Vector2 neighbourPosition) => Block.OnNeighbourChanged(world, position, neighbourPosition);

	private Rectangle GetBlockDestroySourceRectangle()
	{
		var oneSixth = Block.Hardness / 6;

		if (Damage == 0) return Rectangle.Empty;
		if (Damage <= oneSixth) return BlockRectangles.DestroyOne;
		if (Damage <= oneSixth * 2) return BlockRectangles.DestroyTwo;
		if (Damage <= oneSixth * 3) return BlockRectangles.DestroyThree;
		if (Damage <= oneSixth * 4) return BlockRectangles.DestroyFour;
		if (Damage <= oneSixth * 5) return BlockRectangles.DestroyFive;
		return Damage <= Block.Hardness ? BlockRectangles.DestroySix : Rectangle.Empty;
	}

	public virtual void Update()
	{
		_breakingAnimation.Update();
	}
	
	public virtual void Draw(SpriteBatch spriteBatch, Rectangle destRectangle, Rectangle sourceRectangle)
	{
		if (_breakingAnimation.IsAnimating)
		{
			var scale = _breakingAnimation.CurrentScale;
			var scaledWidth = (int) (destRectangle.Width * scale);
			var scaledHeight = (int) (destRectangle.Height * scale);
			// center
			var offsetX = (destRectangle.Width - scaledWidth) / 2;
			var offsetY = (destRectangle.Height - scaledHeight) / 2;
			var finalDestRect = new Rectangle(destRectangle.X + offsetX, destRectangle.Y + offsetY, scaledWidth, scaledHeight);
			Block.Draw(spriteBatch, finalDestRect, sourceRectangle);
		}
		else
		{
			Block.Draw(spriteBatch, destRectangle, sourceRectangle);
		}

		var blockDestroySourceRectangle = GetBlockDestroySourceRectangle();
		spriteBatch.Draw(BlastiaGame.BlockDestroyTexture, destRectangle, blockDestroySourceRectangle, Color.White);
	}
}