using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
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
		// zoom
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
				blockInstance.Draw(spriteBatch, destRect, sourceRect);				
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