using BlasterMaster.Main.Blocks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.GameState;

public class World 
{
	private WorldState _state;
	private Camera _camera;
	
	public World(WorldState state, Camera renderCamera) 
	{
		_state = state;
		_camera = renderCamera;
	}
	
	private void RenderTiles(SpriteBatch spriteBatch) 
	{
		int startX = Math.Max(0, (int) (_camera.Position.X / Block.Size));
		int startY = Math.Max(0, (int) (_camera.Position.Y / Block.Size));
		int endX = Math.Min(_state.WorldWidth, startX + (_camera.DrawWidth / Block.Size));
		int endY = Math.Min(_state.WorldHeight, startY + (_camera.DrawHeight / Block.Size));
		
		for (int x = startX; x < endX; x++) 
		{
			for (int y = startY; y < endY; y++) 
			{
				ushort tileId = _state.GetTile(x, y);
				if (tileId == 0) continue; // skip empty
				
				Texture2D? tileTexture = BlockRegistry.GetTexture(tileId);
				if (tileTexture == null) continue;
				
				Vector2 tilePosition = new Vector2(x * Block.Size - _camera.Position.X,
						y * Block.Size - _camera.Position.Y);
						
				Rectangle sourceRect = BlockRectangles.Middle;
				
				spriteBatch.Draw(tileTexture, tilePosition, sourceRect, Color.White);				
			}
		}
	}
	
	public void Draw(SpriteBatch spriteBatch) 
	{
		RenderTiles(spriteBatch);
	}
}