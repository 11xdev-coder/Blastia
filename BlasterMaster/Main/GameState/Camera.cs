using System.Dynamic;
using BlasterMaster.Main.Blocks.Common;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.GameState;

public class Camera : Object
{
	public Vector2 Position;
	
	private Rectangle _renderRectangle;
	public Rectangle RenderRectangle 
	{
		get => _renderRectangle;
		set => Properties.OnValueChangedProperty<Rectangle>(ref _renderRectangle, value, UpdateRenderRectangle);
	}
	
	/// <summary>
	/// in blocks *8 (tile_size = 8)
	/// </summary>
	public int DrawWidth;
	/// <summary>
	/// in blocks *8 (tile_size = 8)
	/// </summary>
	public int DrawHeight;
	
	public int WorldScale = 5;
	
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
		
	}

	protected override void Draw()
	{
		
	}
	
	public void RenderWorld(SpriteBatch spriteBatch, WorldState worldState) 
	{
		int startX = Math.Max(0, (int) (Position.X / Block.Size));
		int startY = Math.Max(0, (int) (Position.Y / Block.Size));
		int endX = Math.Min(worldState.WorldWidth, startX + (DrawWidth / Block.Size));
		int endY = Math.Min(worldState.WorldHeight, startY + (DrawHeight / Block.Size));
		
		int scaledBlockSize = Block.Size * WorldScale;
		
		for (int x = startX; x < endX; x++) 
		{
			for (int y = startY; y < endY; y++) 
			{
				ushort tileId = worldState.GetTile(x, y);
				if (tileId == 0) continue; // skip empty
				
				Texture2D? tileTexture = BlockRegistry.GetTexture(tileId);
				if (tileTexture == null) continue;
				
				float tilePositionX = (x * Block.Size - Position.X) * WorldScale;
				float tilePositionY = (y * Block.Size - Position.Y) * WorldScale;
				Rectangle destRect = new Rectangle((int) tilePositionX, (int) tilePositionY, 
								scaledBlockSize, scaledBlockSize);
						
				Rectangle sourceRect = BlockRectangles.All;
				
				spriteBatch.Draw(tileTexture, destRect, sourceRect, Color.White);				
			}
		}
	}
}