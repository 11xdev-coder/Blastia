using System.Dynamic;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;

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
}