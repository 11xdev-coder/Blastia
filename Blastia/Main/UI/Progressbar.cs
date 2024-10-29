using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class Progressbar : Image 
{
	private float _progress;
	public float Progress 
	{
		get => _progress;
		set => _progress = Math.Clamp(value, 0f, 1f);
	}
		
	private Color _fillColor;
	private int _horizontalOffset = 5;
	private int _verticalOffset = 5;
	
	public Progressbar(Vector2 position, Texture2D backgroundTexture, 
			Color fillColor = default) : base(position, backgroundTexture, default) 
	{
		// if fillColor = default -> Green; otherwise -> fillColor
		_fillColor = fillColor == default ? Color.Lime : fillColor;
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		base.Draw(spriteBatch);
		
		Rectangle fillRectangle = new Rectangle(Bounds.X + _horizontalOffset, Bounds.Y + _verticalOffset, 
						(int)((Bounds.Width - 2 * _horizontalOffset) * Progress), 
						Bounds.Height - 2 * _verticalOffset);
						
		spriteBatch.Draw(BlastiaGame.WhitePixel, fillRectangle, _fillColor);
	}
}