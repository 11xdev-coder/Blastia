using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Utilities;

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
        using (FileStream fs = File.OpenRead(texturePath))
        {
            return Texture2D.FromStream(graphicsDevice, fs);
        }
    }

    // public static SpriteFont LoadFont(GraphicsDevice graphicsDevice, string fontPath)
    // {
    //     using (FileStream fs = File.OpenRead(fontPath))
    //     {
    //         return SpriteFont.F
    //     }
    // }
}