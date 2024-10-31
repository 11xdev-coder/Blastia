using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.GameState;

public abstract class Object
{
	public Vector2 Position;
	public float Scale = 1f;
	
	public abstract void Update();
	public abstract void Draw(SpriteBatch spriteBatch);
}