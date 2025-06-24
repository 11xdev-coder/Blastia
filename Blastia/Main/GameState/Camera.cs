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
			var clamped = Math.Clamp(value, 10, 9999);
			Properties.OnValueChangedProperty(ref _cameraScale, clamped, OnZoomed);
		}
	}
	private const float ScaleSpeed = 15f;

	public bool IsPlayerBlocked { get; set; }
	
	// world tile position -> block instance (only drawn ones)
	private Dictionary<(Vector2, TileLayer), BlockInstance> _drawnTiles = new();
	private Dictionary<Vector2, Rectangle> _drawnBoxes = new();
	private readonly TileLayer[] _layers;
	
	/// <summary>
	/// Event called whenever <c>CameraScale</c> changes
	/// </summary>
	public Action<float>? OnZoomed;

	public Camera(Vector2 position) 
	{
		Position = position;
		_layers = [TileLayer.Ground, TileLayer.Liquid, TileLayer.Furniture];
	}

	public override void Update()
	{
		// dont zoom if player input is blocked
		if (IsPlayerBlocked) return;
		
		float zoom = 0;
		KeyboardHelper.AccumulateValueFromMap(_zoomMap, ref zoom);
		
		CameraScale += zoom * ScaleSpeed * (float) BlastiaGame.GameTimeElapsedSeconds;
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		
	}
	
	public override void Draw(SpriteBatch spriteBatch, Vector2 scaledPosition, float scale = 1f)
	{
		
	}

	public (Dictionary<Vector2, Rectangle>, Dictionary<(Vector2, TileLayer), BlockInstance>) SetDrawnTiles(WorldState worldState)
	{
		_drawnBoxes.Clear();
		_drawnTiles.Clear();
		
		int viewLeft = (int)Math.Floor(Position.X / Block.Size);
		int viewTop = (int)Math.Floor(Position.Y / Block.Size);
    
		// Calculate how many tiles fit in view
		int scaledBlockSize = (int)(Block.Size * CameraScale);
		int tilesHorizontal = DrawWidth / scaledBlockSize + 1;
		int tilesVertical = DrawHeight / scaledBlockSize + 1;

		int lastTileX = viewLeft + tilesHorizontal;
		int lastTileY = viewTop + tilesVertical;
		
		// clamp
		viewLeft = Math.Max(0, viewLeft);
		viewTop = Math.Max(0, viewTop);
		lastTileX = Math.Min(worldState.WorldWidth / Block.Size, lastTileX);
		lastTileY = Math.Min(worldState.WorldHeight / Block.Size, lastTileY);
		
		// go through each tile
		for (int x = viewLeft; x < lastTileX; x++)
		{
			for (int y = viewTop; y < lastTileY; y++)
			{
				int worldXCoord = x * Block.Size;
				int worldYCoord = y * Block.Size;
				var position = new Vector2(worldXCoord, worldYCoord);

				foreach (var layer in _layers)
				{
					var blockInstance = worldState.GetBlockInstance(worldXCoord, worldYCoord, layer);
					if (blockInstance == null) continue; // skip air
					
					_drawnTiles.Add((position, layer), blockInstance);

					if (!_drawnBoxes.ContainsKey(position))
					{
						var tileBox = new Rectangle(worldXCoord, worldYCoord, Block.Size, Block.Size);
						_drawnBoxes.Add(position, tileBox);
					}
				}
			}
		}

		return (_drawnBoxes, _drawnTiles);
	}
	
	public void RenderGroundTiles(SpriteBatch spriteBatch, WorldState worldState)
	{
		int scaledBlockSize = (int)(Block.Size * CameraScale);

		var groundTiles = _drawnTiles.Where(kvp => kvp.Key.Item2 == TileLayer.Ground);
		
		// ground 
		RenderTileLayer(spriteBatch, worldState, groundTiles, scaledBlockSize);
	}

	public void RenderFurnitureThenLiquids(SpriteBatch spriteBatch, WorldState worldState)
	{
		int scaledBlockSize = (int)(Block.Size * CameraScale);
		
		var furnitureTiles = _drawnTiles.Where(kvp => kvp.Key.Item2 == TileLayer.Furniture);
		var liquidTiles = _drawnTiles.Where(kvp => kvp.Key.Item2 == TileLayer.Liquid);
		
		// furniture -> liquids
		RenderTileLayer(spriteBatch, worldState, furnitureTiles, scaledBlockSize);
		RenderTileLayer(spriteBatch, worldState, liquidTiles, scaledBlockSize);
	}

	private void RenderTileLayer(SpriteBatch spriteBatch, WorldState worldState,
		IEnumerable<KeyValuePair<(Vector2, TileLayer), BlockInstance>> tiles, int scaledBlockSize)
	{
		foreach (var kvp in tiles)
		{
			var tilePosition = kvp.Key.Item1;
			var layer = kvp.Key.Item2;
			var blockInstance = kvp.Value;
			
			var worldXCoord = (int) tilePosition.X;
			var worldYCoord = (int) tilePosition.Y;
			
			// subtract camera position -> scrolling (camera moves right -> move tile to the left)
			float worldPositionX = worldXCoord - Position.X;
			float worldPositionY = worldYCoord - Position.Y;
			Rectangle destRect = new Rectangle(MathUtilities.SmoothRound(worldPositionX * CameraScale), 
				MathUtilities.SmoothRound(worldPositionY * CameraScale), 
				scaledBlockSize, scaledBlockSize);

			var topTile = worldState.GetBlockInstance(worldXCoord, worldYCoord - 8, layer);
			var bottomTile = worldState.GetBlockInstance(worldXCoord, worldYCoord + 8, layer);
			var rightTile = worldState.GetBlockInstance(worldXCoord + 8, worldYCoord, layer);
			var leftTile = worldState.GetBlockInstance(worldXCoord - 8, worldYCoord, layer);
			Rectangle sourceRect = blockInstance.Block.GetRuleTileSourceRectangle(topTile == null || topTile.Block.IsTransparent, 
				bottomTile == null || bottomTile.Block.IsTransparent, 
				rightTile == null || rightTile.Block.IsTransparent, 
				leftTile == null || leftTile.Block.IsTransparent);
			
			blockInstance.Draw(spriteBatch, destRect, sourceRect, tilePosition);
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

	    var font = BlastiaGame.MainFont;
	    if (font == null) return;

	    var textColor = new Color(255, 255, 255, 128);
	    var textScale = Math.Max(0.6f, Math.Min(0.8f, (int) (1 * CameraScale / 5)));

	    for (var x = firstCellX; x <= lastCellX; x++)
	    {
		    for (var y = firstCellY; y <= lastCellY; y++)
		    {
			    // cell pos in world coordinates
			    var cellPos = new Vector2(x * Collision.CellSize, y * Collision.CellSize);

			    var collidablesCount = 0;
			    var entitiesCount = 0;
			    if (Collision.Cells.TryGetValue(cellPos, out var entityList))
			    {
				    var collidables = entityList.Where(e => e.IsCollidable);
				    var entities = entityList.Where(e => !e.IsCollidable);
				    collidablesCount = collidables.Count();
				    entitiesCount = entities.Count();
			    }
			    
			    var screenPos = WorldToScreen(cellPos);
			    var screenPosWithOffset = WorldToScreen(new Vector2(cellPos.X, cellPos.Y + 3));
			    spriteBatch.DrawString(font, $"col.: {collidablesCount.ToString()}", screenPos + new Vector2(lineThickness + 2),
				    textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
			    spriteBatch.DrawString(font, $"entities: {entitiesCount.ToString()}", screenPosWithOffset + new Vector2(lineThickness + 2),
				    textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
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