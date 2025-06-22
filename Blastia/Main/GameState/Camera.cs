using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.Physics;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.GameState;

public class Camera : Object
{
	private readonly Dictionary<Keys, float> _zoomMap = new()
	{
		{Keys.OemPlus, 1},
		{Keys.OemMinus, -1}
	};
	
	/// <summary>
	/// in blocks *8 (tile_size = 8)
	/// </summary>
	public int DrawWidth;
	/// <summary>
	/// in blocks *8 (tile_size = 8)
	/// </summary>
	public int DrawHeight;

	// ZOOM/SCALE
	private float _cameraScale = 10;
	public float CameraScale
	{
		get => _cameraScale;
		private set
		{
			var clamped = Math.Clamp(value, 1, 9999);
			Properties.OnValueChangedProperty(ref _cameraScale, clamped, OnZoomed);
		}
	}
	private const float ScaleSpeed = 0.25f;

	public bool IsPlayerBlocked { get; set; }
	
	/// <summary>
	/// Event called whenever <c>CameraScale</c> changes
	/// </summary>
	public Action<float>? OnZoomed;

	public Camera(Vector2 position) 
	{
		Position = position;
	}

	public override void Update()
	{
		// dont zoom if player input is blocked
		if (IsPlayerBlocked) return;
		
		float zoom = 0;
		KeyboardHelper.AccumulateValueFromMap(_zoomMap, ref zoom);
		
		CameraScale += zoom * ScaleSpeed;
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		
	}
	
	public override void Draw(SpriteBatch spriteBatch, Vector2 scaledPosition, float scale = 1f)
	{
		
	}
	
	public void RenderWorld(SpriteBatch spriteBatch, WorldState worldState)
	{
		int firstTileX = (int) Math.Floor(Position.X / Block.Size);
		int firstTileY = (int) Math.Floor(Position.Y / Block.Size);

		// num of tiles visible
		int numTilesX = (int) Math.Ceiling(DrawWidth / CameraScale / Block.Size) + 1;
		int numTilesY = (int) Math.Ceiling(DrawHeight / CameraScale / Block.Size) + 1;

		int lastTileX = firstTileX + numTilesX;
		int lastTileY = firstTileY + numTilesY;
		
		// clamp
		firstTileX = Math.Max(0, firstTileX);
		firstTileY = Math.Max(0, firstTileY);
		lastTileX = Math.Min(worldState.WorldWidth / Block.Size, lastTileX);
		lastTileY = Math.Min(worldState.WorldHeight / Block.Size, lastTileY);
		
		int scaledBlockSize = MathUtilities.SmoothRound(Block.Size * CameraScale);
		
		// go through each tile
		for (int x = firstTileX; x < lastTileX; x++) 
		{
			for (int y = firstTileY; y < lastTileY; y++)
			{
				int worldXCoord = x * Block.Size;
				int worldYCoord = y * Block.Size;
				
				var blockInstance = worldState.GetBlockInstance(worldXCoord, worldYCoord);
				if (blockInstance == null) continue; // skip air
				
				// subtract camera position -> scrolling (camera moves right -> move tile to the left)
				float worldPositionX = worldXCoord - Position.X;
				float worldPositionY = worldYCoord - Position.Y;
				Rectangle destRect = new Rectangle(MathUtilities.SmoothRound(worldPositionX * CameraScale), 
					MathUtilities.SmoothRound(worldPositionY * CameraScale), 
					scaledBlockSize, scaledBlockSize);

				var topTile = worldState.GetBlockInstance(worldXCoord, worldYCoord - 8);
				var bottomTile = worldState.GetBlockInstance(worldXCoord, worldYCoord + 8);
				var rightTile = worldState.GetBlockInstance(worldXCoord + 8, worldYCoord);
				var leftTile = worldState.GetBlockInstance(worldXCoord - 8, worldYCoord);
				Rectangle sourceRect = blockInstance.Block.GetRuleTileSourceRectangle(topTile == null || topTile.Block.IsTransparent, 
					bottomTile == null || bottomTile.Block.IsTransparent, 
					rightTile == null || rightTile.Block.IsTransparent, 
					leftTile == null || leftTile.Block.IsTransparent);
				blockInstance.Draw(spriteBatch, destRect, sourceRect, new Vector2(worldXCoord, worldYCoord));				
			}
		}
	}

	public void RenderEntity(SpriteBatch spriteBatch, Entity entity)
	{
		// scrolling offset
		float playerX = entity.Position.X - Position.X;
		float playerY = entity.Position.Y - Position.Y;

		float scaledPositionX = playerX * CameraScale;
		float scaledPositionY = playerY * CameraScale;
		
		Vector2 scaledPosition = new Vector2(scaledPositionX, scaledPositionY);
		entity.Draw(spriteBatch, scaledPosition, CameraScale);
	}
	
	public void RenderSpatialGrid(SpriteBatch spriteBatch, WorldState worldState)
	{
	    // visible area
	    float visibleWorldWidth = DrawWidth / CameraScale;
	    float visibleWorldHeight = DrawHeight / CameraScale;
	    
	    // draw lines only in camera view
	    int firstCellX = (int)Math.Floor(Position.X / Collision.CellSize);
	    int firstCellY = (int)Math.Floor(Position.Y / Collision.CellSize);
	    int lastCellX = (int)Math.Ceiling((Position.X + visibleWorldWidth) / Collision.CellSize);
	    int lastCellY = (int)Math.Ceiling((Position.Y + visibleWorldHeight) / Collision.CellSize);
	    
	    // clamp to world bounds
	    firstCellX = Math.Max(0, firstCellX);
	    firstCellY = Math.Max(0, firstCellY);
	    lastCellX = Math.Min(worldState.WorldWidth / Collision.CellSize + 1, lastCellX);
	    lastCellY = Math.Min(worldState.WorldHeight / Collision.CellSize + 1, lastCellY);
	    
	    var gridColor = new Color(128, 128, 128, 64);
	    
	    // use fixed line thickness, dont scale too much
	    int lineThickness = Math.Max(3, Math.Min(6, (int)(1 * CameraScale / 5)));
	    Texture2D pixelTexture = BlastiaGame.WhitePixel;
	    
	    // vertical lines
	    for (int x = firstCellX; x <= lastCellX; x++)
	    {
	        float worldX = x * Collision.CellSize;
	        // world to screen space
	        float screenX = (float)Math.Round((worldX - Position.X) * CameraScale);
	        
	        // line is within screen bounds
	        if (screenX >= 0 && screenX < DrawWidth)
	        {
	            Rectangle destRect = new Rectangle((int)screenX, 0, lineThickness, DrawHeight);
	            spriteBatch.Draw(pixelTexture, destRect, gridColor);
	        }
	    }
	    
	    // horizontal lines
	    for (int y = firstCellY; y <= lastCellY; y++)
	    {
	        float worldY = y * Collision.CellSize;
	        // world to screen space
	        float screenY = (float)Math.Round((worldY - Position.Y) * CameraScale);
	        
	        // line is within bounds
	        if (screenY >= 0 && screenY < DrawHeight)
	        {
	            Rectangle destRect = new Rectangle(0, (int)screenY, DrawWidth, lineThickness);
	            spriteBatch.Draw(pixelTexture, destRect, gridColor);
	        }
	    }
	}

	public Vector2 ScreenToWorld(Vector2 screenPosition)
	{
		Vector2 unscaledPosition = screenPosition / CameraScale;
		
		return unscaledPosition + Position;
	}
	
	public Vector2 WorldToScreen(Vector2 worldPosition)
	{
		return new Vector2(
			(worldPosition.X - Position.X) * CameraScale,
			(worldPosition.Y - Position.Y) * CameraScale
		);
	}
}