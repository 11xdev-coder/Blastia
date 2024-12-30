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
	private float _cameraScale = 1;
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
	
	// TODO: Issue: low resolution rendering problems
	public void RenderWorld(SpriteBatch spriteBatch, WorldState worldState) 
	{
		// get corners in tiles
		int startX = Math.Max(0, (int) Position.X);
		int startY = Math.Max(0, (int) Position.Y);
		int endX = Math.Min(worldState.WorldWidth, startX + DrawWidth);
		int endY = Math.Min(worldState.WorldHeight, startY + DrawHeight);
		
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
				float worldPositionX = x - Position.X;
				float worldPositionY = y - Position.Y;
				Rectangle destRect = new Rectangle(MathUtilities.SmoothRound(worldPositionX * CameraScale), 
					MathUtilities.SmoothRound(worldPositionY * CameraScale), 
					scaledBlockSize, scaledBlockSize);
						
				Rectangle sourceRect = BlockRectangles.All;
				
				block.Draw(spriteBatch, destRect, sourceRect);				
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