using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Utilities;

public class TextureManager 
{
    private GraphicsDevice _graphicsDevice;
    private List<string> _workingFolders = ["Blocks", "UI", "Menu", "UI/WorldCreation"];
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
}