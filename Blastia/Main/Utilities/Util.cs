using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Utilities;

public static class Util
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

    public static void Shuffle<T>(List<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n -= 1;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]); // swap
        }
    }
}