using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks.Common;

[Serializable]
public abstract class Block 
{
	public static readonly int Size = 8;
	
	public abstract ushort ID { get; }

	public virtual void Draw(SpriteBatch spriteBatch, Rectangle destRectangle, Rectangle sourceRectangle)
	{
		var texture = StuffRegistry.GetTexture(ID);
		spriteBatch.Draw(texture, destRectangle, sourceRectangle, Color.White);
	}
}