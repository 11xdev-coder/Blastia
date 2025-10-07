using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Blastia.Main.Utilities;

public struct GifData 
{
    public Texture2D[] Frames;
    public float[] Durations;
}

public static class Util
{
    private static Func<GraphicsDevice>? _graphicsDeviceFactory;
    private static readonly HttpClient _httpClient = new();
    private static readonly Dictionary<string, GifData> _loadedGifs = [];
    
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
    
    public static void Init(Func<GraphicsDevice> graphicsDeviceFactory) 
    {
        _graphicsDeviceFactory = graphicsDeviceFactory;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="url">Website url</param>
    /// <returns><c>GifData</c> containing frames and their durations</returns>
    public static async Task<GifData?> DownloadAndProcessGif(string url) 
    {
        if (_graphicsDeviceFactory == null) return null;
        
        try 
        {
            if (_loadedGifs.TryGetValue(url, out var loadedGif))
                return loadedGif;

            Console.WriteLine($"Retrieving GIF from URL: {url}");

            // for this to work with all websites, we need to extract the GIF URL from the website
            var gifUrl = await GetGifUrl(url);
            if (string.IsNullOrEmpty(gifUrl))
            {
                Console.WriteLine("Could not extract GIF URL");
                return null;
            }

            // download
            byte[] gifData = await _httpClient.GetByteArrayAsync(gifUrl);            
            // extract frames
            var result = ProcessGif(_graphicsDeviceFactory(), gifData);
            
            if (result.HasValue) 
            {
                _loadedGifs[url] = result.Value;
            }

            return result;
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Error retrieving GIF: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Extracts actual GIF url from the website url
    /// </summary>
    private static async Task<string> GetGifUrl(string url) 
    {
        if (url.Contains("tenor.com"))
            return await GetGifUrlFromTenor(url);

        return url;
    }
    
    /// <summary>
    /// Extracts actual GIF url from <c>tenor.com</c>
    /// </summary>
    private static async Task<string> GetGifUrlFromTenor(string tenorUrl) 
    {
        try 
        {
            var html = await _httpClient.GetStringAsync(tenorUrl);

            // look for tenor media URLs in the HTML
            // pattern 1: looks for Open Graph meta tags
            // <meta property="og:image" content=" - matches this exact HTML text
            // ( - starts capture group
            // [^" - means any character except "
            // + - means one or more times
            // \.gif - mathes .gif
            
            // pattern 2: similar to pattern 1, but looks for og:url instead
            
            // pattern 3: looks for GIF urls in JSON data
            // "url" - matches the text "url"
            // \s* - matches 0 or more whitespace chars
            // : - matches colon
            // \s* matches more whitespace chars
            // https?:// - matches http:// or https:// (? makes S optional)
            // [^""]+ - means any character except "
            // \.gif - ".gif"
            
            // pattern 4: looks for standard HTML image tags
            // src=" - matches src attribute
            // (https?://[^""]+\.gif) - full GIF url ending in .gif
            var patterns = new[]
            {
                @"<meta property=""og:image"" content=""([^""]+\.gif)""",
                @"<meta property=""og:url"" content=""([^""]+\.gif)""",
                @"""url""\s*:\s*""(https?://[^""]+\.gif)""",
                @"src=""(https?://[^""]+\.gif)"""
            };
            
            foreach (var pattern in patterns) 
            {
                // search and extract these patterns
                var match = System.Text.RegularExpressions.Regex.Match(html, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // match.Groups[0] - same as match.Value
                // match.Groups[1] - the URL we need
                
                if (match.Success && match.Groups.Count > 1)
                {
                    var url = match.Groups[1].Value;
                    if (!url.Contains("thumb"))
                        return url;
                }
            }
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Error extracting GIF URL from tenor.com: {ex.Message}");
        }

        return "";
    }
    
    private static GifData? ProcessGif(GraphicsDevice graphicsDevice, byte[] bytes) 
    {
        try 
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(bytes);
            var frames = new List<Texture2D>();
            var durations = new List<float>();

            // get GIF metadata
            var meta = image.Metadata.GetGifMetadata();
            
            for (int i = 0; i < image.Frames.Count; i++) 
            {
                var frame = image.Frames[i];
                var frameMeta = frame.Metadata.GetGifMetadata();

                // convert frame to Texture2D
                var texture = FrameToTexture2D(graphicsDevice, frame);
                frames.Add(texture);

                // get frame duration (1/100th seconds)
                float duration = frameMeta.FrameDelay / 100f;
                if (duration <= 0) duration = 0.1f;
                durations.Add(duration);
            }

            return new GifData
            {
                Frames = frames.ToArray(),
                Durations = durations.ToArray()
            };
            
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Error decoding GIF: {ex.Message}");
            return null;
        }
    }
    
    private static Texture2D FrameToTexture2D(GraphicsDevice graphicsDevice, ImageFrame<Rgba32> frame) 
    {
        var texture = new Texture2D(graphicsDevice, frame.Width, frame.Height);
        var pixelData = new Microsoft.Xna.Framework.Color[frame.Width * frame.Height];

        frame.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++) 
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++) 
                {
                    var pixel = row[x];
                    pixelData[y * frame.Width + x] = new Microsoft.Xna.Framework.Color(pixel.R, pixel.G, pixel.B, pixel.A);
                }
            }
        });

        texture.SetData(pixelData);
        return texture;
    }
}