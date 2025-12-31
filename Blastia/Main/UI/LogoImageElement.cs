using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class LogoImageElement : Image
{
	private Text? _hintText;

	private const float ScaleSpeed = 2f;
	private const float MinScale = 0.8f;
	private const float MaxScale = 1.2f;
	private float _scaleTimer;
	
	public LogoImageElement(Vector2 position, Texture2D texture) : base(position, texture)
	{
		if (BlastiaGame.MainFont == null) return;

		Font = BlastiaGame.MainFont;
		_hintText = new Text(Vector2.Zero, "pineapple!", Font)
		{
			DrawColor = BlastiaGame.ErrorColor
		};
	}

    public override void Update()
    {
        base.Update();

		if (_hintText == null || Font == null) return;

		_hintText.DrawColor = BlastiaGame.ErrorColor;

		_scaleTimer += (float)BlastiaGame.GameTime.ElapsedGameTime.TotalSeconds * ScaleSpeed;
        
        // scale using sine wave
        var normalizedSine = (MathF.Sin(_scaleTimer) + 1f) / 2f; // convert from [-1,1] to [0,1]
        var currentScale = MathHelper.Lerp(MinScale, MaxScale, normalizedSine);
        _hintText.Scale = new Vector2(currentScale);

		var size = Font.MeasureString(_hintText.Text);
		_hintText.Position = new Vector2(Bounds.Right - size.X - 20, Bounds.Bottom - size.Y);
		_hintText?.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
		_hintText?.Draw(spriteBatch);
    }
}