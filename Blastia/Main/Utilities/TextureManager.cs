using Assimp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Utilities;

public class TextureManager 
{
    private GraphicsDevice _graphicsDevice;
    private List<string> _workingFolders = ["Blocks", "UI", "Menu", "UI/WorldCreation", "UI/Icons", "UI/Background"];
    private Dictionary<string, Texture2D> _textures = [];
    private Texture2D? _invisibleTexture;
    private Texture2D? _whitePixelTexture;
    
    public TextureManager(string contentRoot, GraphicsDevice graphicsDevice) 
    {
        _graphicsDevice = graphicsDevice;
        
        foreach (var folder in _workingFolders) 
        {
            var absoluteFolderPath = Path.Combine(contentRoot, folder);
            var allPngs = Directory.GetFiles(absoluteFolderPath, "*.png");
            foreach (var png in allPngs) 
            {
                // get texture name and combine with folder
                var textureName = Path.GetFileNameWithoutExtension(png);
                var key = $"{folder}/{textureName}";
                
                var texture = Util.LoadTexture(graphicsDevice, png);
                
                _textures.Add(key, texture);
            }
        }
    }
    
    /// <summary>
    /// Gets texture based on name and categories
    /// </summary>
    /// <param name="texture">Texture name</param>
    /// <param name="categories">Folders where the texture lays (e.g. path <c>"UI/WorldCreation"</c> so categories should be <c>"UI"</c>, <c>"WorldCreation"</c>)</param>
    /// <returns></returns>
    public Texture2D Get(string texture, params string[] categories) 
    {
        // combine texture name with categories
        var allParts = categories.Append(texture).ToArray();
        var fullCategory = string.Join("/", allParts);
        return _textures[fullCategory];
    }
    
    public Texture2D Invisible() 
    {
        if (_invisibleTexture == null) 
        {
            _invisibleTexture = new Texture2D(_graphicsDevice, 1, 1);
            _invisibleTexture.SetData([Color.Transparent]);
        }
        return _invisibleTexture;
    }
    
    public Texture2D WhitePixel() 
    {
        if (_whitePixelTexture == null) 
        {
            _whitePixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _whitePixelTexture.SetData([Color.White]);
        }
        return _whitePixelTexture;
    }
    
    public Texture2D Rescale(Texture2D tex, Vector2 size) 
    {
        // SetData and GetData use 1d arrays for texture pixels
        // [0] - index
        // (0,0) [0] | (1,0) [1] | (2,0) [2]
        // (0,1) [3] | (1,1) [4] | (2,1) [5]
        // (0,2) [6] | (1,2) [7] | (2,2) [8]
        // by accessing arr[i], CPU loads nearby memory and loads arr[i+1], so its faster to follow the intended layout
        // 1. iterate: inner loop - columns
        // 2. map rescaled pixels to nearby source coordinates
        // 3. set new texture
        
        Color[] originalData = new Color[tex.Width * tex.Height];
        tex.GetData(originalData);
        
        int newWidth = (int)(tex.Width * size.X);
        int newHeight = (int)(tex.Height * size.Y);
        Color[] newData = new Color[newWidth * newHeight];
        
        for (int row = 0; row < newWidth; row++) 
        {
            for (int col = 0; col < newWidth; col++) 
            {
                int srcCol = (int)(col * tex.Width / newWidth);
                int srcRow = (int)(row * tex.Height / newHeight);
                
                srcCol = Math.Min(srcCol, tex.Width - 1);
                srcRow = Math.Min(srcRow, tex.Height - 1);
                
                // skip Y rows, then add X
                // index = Y * width + X
                newData[row * newWidth + col] = originalData[srcRow * tex.Width + srcCol];
            }
        }
        
        // create new texture
        Texture2D newTexture = new Texture2D(_graphicsDevice, newWidth, newHeight);
        newTexture.SetData(newData);
        
        return newTexture;
    }
}