using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Utilities;

public static class LoadingUtilities
{
    /// <summary>
    /// Loads .png texture from stream
    /// </summary>
    /// <param name="graphicsDevice"></param>
    /// <param name="texturePath"></param>
    /// <returns></returns>
    public static Texture2D LoadTexture(GraphicsDevice graphicsDevice, string texturePath)
    {
        using FileStream fs = new FileStream(texturePath, FileMode.Open);
        return Texture2D.FromStream(graphicsDevice, fs);
    }
}