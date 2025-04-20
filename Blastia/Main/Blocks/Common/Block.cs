using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks.Common;

[Serializable]
public abstract class Block 
{
	public static readonly int Size = 8;
	public static readonly float AirDragCoefficient = 1f;
	
	public abstract ushort ID { get; }
	/// <summary>
	/// How much drag to apply when entity walks on this block. 0 = no drag force
	/// </summary>
	public abstract float DragCoefficient { get; }

	public virtual void Draw(SpriteBatch spriteBatch, Rectangle destRectangle, Rectangle sourceRectangle)
	{
		var texture = StuffRegistry.GetTexture(ID);
		spriteBatch.Draw(texture, destRectangle, sourceRectangle, Color.White);
	}
}