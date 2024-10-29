using Microsoft.Xna.Framework;

namespace Blastia.Main.GameState;

public abstract class Object 
{
	public Vector2 Position;
	public abstract void Update();
	protected abstract void Draw();
}