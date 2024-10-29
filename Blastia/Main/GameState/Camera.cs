using Blastia.Main.Blocks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
	
	public int CameraScale = 5;
	
	public Camera(Vector2 position) 
	{
		Position = position;
		UpdateRenderRectangle();
	}
	
	private void UpdateRenderRectangle() 
	{
		RenderRectangle = new Rectangle((int) Position.X, (int) Position.Y, DrawWidth, DrawHeight);
	}

	protected override void Update()
	{
		UpdateRenderRectangle();
	}

	protected override void Draw()
	{
		
	}
	
	public void RenderWorld(SpriteBatch spriteBatch, WorldState worldState) 
	{
		// get corners in tiles
		int startX = Math.Max(0, (int) (Position.X / Block.Size));
		int startY = Math.Max(0, (int) (Position.Y / Block.Size));
		int endX = Math.Min(worldState.WorldWidth, startX + (DrawWidth / Block.Size));
		int endY = Math.Min(worldState.WorldHeight, startY + (DrawHeight / Block.Size));
		
		int scaledBlockSize = Block.Size * CameraScale;
		
		// go through each tile
		for (int x = startX; x < endX; x++) 
		{
			for (int y = startY; y < endY; y++) 
			{
				ushort tileId = worldState.GetTile(x, y);
				if (tileId == 0) continue; // skip empty
				
				Texture2D? tileTexture = BlockRegistry.GetTexture(tileId);
				if (tileTexture == null) continue;
				
				// subtract camera position -> scrolling (camera moves right -> move tile to the left)
				float worldPositionX = x * Block.Size - Position.X;
				float worldPositionY = y * Block.Size - Position.Y;
				Rectangle destRect = new Rectangle((int) worldPositionX * CameraScale, (int) worldPositionY * CameraScale, 
								scaledBlockSize, scaledBlockSize);
						
				Rectangle sourceRect = BlockRectangles.All;
				
				spriteBatch.Draw(tileTexture, destRect, sourceRect, Color.White);				
			}
		}
	}
}