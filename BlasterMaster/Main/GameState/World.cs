using BlasterMaster.Main.Blocks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.GameState;

public class World 
{
	private WorldState _state;
	private Camera _camera;
	private const int TILE_SIZE = 16;
	
	public World(WorldState state, Camera renderCamera) 
	{
		_state = state;
		_camera = renderCamera;
	}
	
	private void RenderTiles(SpriteBatch spriteBatch) 
	{
		int startX = Math.Max(0, (int) (_camera.Position.X / TILE_SIZE));
		int startY = Math.Max(0, (int) (_camera.Position.Y / TILE_SIZE));
		int endX = Math.Min(_state.WorldWidth, startX + (_camera.DrawWidth / TILE_SIZE) + 2);
		int endY = Math.Min(_state.WorldHeight, startY + (_camera.DrawHeight / TILE_SIZE) + 2);
		
		Console.WriteLine($"startX calculation: {_camera.Position.X} / {TILE_SIZE} = {_camera.Position.X / TILE_SIZE}");
	Console.WriteLine($"endX calculation: {startX} + ({_camera.DrawWidth} / {TILE_SIZE}) + 2 = {startX + (_camera.DrawWidth / TILE_SIZE) + 2}");
	Console.WriteLine($"World Width: {_state.WorldWidth}");
		for (int x = startX; x < endX; x++) 
		{
			for (int y = startY; y < endY; y++) 
			{
				ushort tileId = _state.GetTile(x, y);
				Console.WriteLine(tileId);
				if (tileId == 0) continue; // skip empty
				
				Console.WriteLine("ID is not 0");
				Texture2D? tileTexture = BlockRegistry.GetTexture(tileId);
				if (tileTexture == null) continue;
				
				Console.WriteLine("TExture is not null");
				Vector2 tilePosition = new Vector2(x * TILE_SIZE - _camera.Position.X,
						y * TILE_SIZE - _camera.Position.Y);
						
				spriteBatch.Draw(tileTexture, tilePosition, null, Color.White);				
			}
		}
	}
	
	public void Draw(SpriteBatch spriteBatch) 
	{
		RenderTiles(spriteBatch);
	}
}