using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.GameState;

public abstract class Object
{
	public Action<Object>? OnPositionChanged;
	private Vector2 _position;
	public Vector2 Position
	{
		get => _position;
		set
		{
			_position = value;
			OnPositionChanged?.Invoke(this);
		}
	}
	public float Scale = 1f;
	
	public abstract void Update();
	public abstract void Draw(SpriteBatch spriteBatch);
	public abstract void Draw(SpriteBatch spriteBatch, Vector2 scaledPosition, float scale = 1f);
}