using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.GameState;

public class Camera : Object
{	
	public Rectangle RenderRectangle;
	
	/// <summary>
	/// in blocks *8 (tile_size = 8)
	/// </summary>
	public int DrawWidth;
	/// <summary>
	/// in blocks *8 (tile_size = 8)
	/// </summary>
	public int DrawHeight;

	private float _cameraScale = 1;
	public float CameraScale
	{
		get => _cameraScale;
		set => _cameraScale = Math.Clamp(value, 1, 9999);
	}
	
	public Camera(Vector2 position) 
	{
		Position = position;
		UpdateRenderRectangle();
	}
	
	private void UpdateRenderRectangle() 
	{
		RenderRectangle = new Rectangle(MathUtilities.SmoothRound(Position.X), MathUtilities.SmoothRound(Position.Y), 
			DrawWidth, DrawHeight);
	}

	public override void Update()
	{
		// zoom
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.OemPlus))
		{
			CameraScale += 0.25f;
		}
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.OemMinus))
		{
			CameraScale -= 0.25f;
		}
		
		UpdateRenderRectangle();
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		
	}
	
	public void RenderWorld(SpriteBatch spriteBatch, WorldState worldState) 
	{
		// get corners in tiles
		int startX = Math.Max(0, (int) (Position.X / Block.Size));
		int startY = Math.Max(0, (int) (Position.Y / Block.Size));
		int endX = Math.Min(worldState.WorldWidth, startX + (DrawWidth / Block.Size));
		int endY = Math.Min(worldState.WorldHeight, startY + (DrawHeight / Block.Size));
		
		int scaledBlockSize = MathUtilities.SmoothRound(Block.Size * CameraScale);
		
		// go through each tile
		for (int x = startX; x < endX; x++) 
		{
			for (int y = startY; y < endY; y++) 
			{
				ushort tileId = worldState.GetTile(x, y);
				if (tileId == 0) continue; // skip empty
				
				Block? block = StuffRegistry.GetBlock(tileId);
				if (block == null) continue;
				
				// subtract camera position -> scrolling (camera moves right -> move tile to the left)
				float worldPositionX = x * Block.Size - Position.X;
				float worldPositionY = y * Block.Size - Position.Y;
				Rectangle destRect = new Rectangle(MathUtilities.SmoothRound(worldPositionX * CameraScale), 
					MathUtilities.SmoothRound(worldPositionY * CameraScale), 
					scaledBlockSize, scaledBlockSize);
						
				Rectangle sourceRect = BlockRectangles.All;
				
				block.Draw(spriteBatch, destRect, sourceRect);				
			}
		}
	}

	public void RenderPlayer(SpriteBatch spriteBatch, Player player)
	{
		// scrolling offset
		float playerX = player.Position.X - Position.X;
		float playerY = player.Position.Y - Position.Y;

		float scaledPositionX = playerX * CameraScale;
		float scaledPositionY = playerY * CameraScale;
		
		Vector2 scaledPosition = new Vector2(scaledPositionX, scaledPositionY);
		player.Draw(spriteBatch, scaledPosition, CameraScale);
	}
}