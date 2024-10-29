using Microsoft.Xna.Framework;

namespace BlasterMaster.Main.GameState;

public abstract class Object 
{
	public Vector2 Position;
	protected abstract void Update();
	protected abstract void Draw();
}