using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class AnimatedGif : UIElement 
{
    private Texture2D[] _frames = [];
    private float[] _frameDurations = [];
    private readonly string _url;
    private readonly bool _loop;
    private int _currentFrame;
    private float _frameTimer;
    private bool _hasLoadedGif;

    public Action? OnGifLoaded;
    
    public AnimatedGif(Vector2 position, string url, bool loop = true, bool resize = false) : base(position, BlastiaGame.InvisibleTexture)
    {
        _url = url;
        _loop = loop;
        _currentFrame = 0;
        _frameTimer = 0f;
        Init(resize);
    }
    
    private async void Init(bool resize) 
    {
        var gifData = await Util.DownloadAndProcessGif(_url);
        if (gifData.HasValue) 
        {
            _frames = gifData.Value.Frames;
            _frameDurations = gifData.Value.Durations;
            _hasLoadedGif = true;
            
            if (_frames.Length > 0) 
            {
                Texture = _frames[0];
                
                // resize if needed
                if (resize)
                    ResizeGif();
                UpdateBounds();

                OnGifLoaded?.Invoke();
            }
            else 
            {
                Console.WriteLine("No frames found for AnimatedGif");
            }
        }
    }
    
    /// <summary>
    /// Resizes gif to reasonable scale (200f width)
    /// </summary>
    private void ResizeGif() 
    {
        if (Texture == null) return;
        
        var targetWidth = 200f;
        var scaleX = targetWidth / Texture.Width;
        var scaleY = scaleX;

        Scale = new Vector2(scaleX, scaleY);
    }

    public override void UpdateBounds()
    {
        if (Texture == null) return;
        UpdateBoundsBase(Texture.Width, Texture.Height);
    }

    public override void Update()
    {
        base.Update();

        if (_frames.Length <= 1 || !_hasLoadedGif) return;    

        var delta = (float)BlastiaGame.GameTimeElapsedSeconds;
        _frameTimer += delta;
        
        if (_frameTimer >= _frameDurations[_currentFrame]) 
        {
            _frameTimer = 0f;
            _currentFrame += 1;
            
            if (_currentFrame >= _frames.Length) 
            {
                if (_loop)
                    _currentFrame = 0; // start over
                else
                    _currentFrame = _frames.Length - 1; // last frame
            }
        }

        // update texture
        var newTexture = _frames[_currentFrame];
        if (newTexture != Texture) 
        {
            Texture = newTexture;
            UpdateBounds();
        }       
    }
}