using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class LogoImageElement : Image
{
	private bool _rotatingBack;
	private bool _scalingBack;
	
	private const float RotationSpeed = 0.05f;
	private const float MaxRotationAngle = 20f;
	private const float MinRotationAngle = -20f;

	private const float ScalingSpeed = 0.05f;
	private const float MinScale = 0.8f;
	private const float MaxScale = 1.8f;

	private Vector2 _scalingSpeedVector;
	
	public LogoImageElement(Vector2 position, Texture2D texture) : base(position, texture)
	{
		_scalingSpeedVector = new Vector2(ScalingSpeed, ScalingSpeed);
		DrawColor = Color.LightGray;
	}

	public override void Update()
	{
		Rotate();
		Resize();

		base.Update();
	}

	private void Rotate()
	{
		var rotationAmount = RotationSpeed * (float) BlastiaGame.GameTimeElapsedSeconds;
		
		if (MathHelper.ToDegrees(Rotation) > MaxRotationAngle)
		{
			_rotatingBack = true;
		}
		else if (MathHelper.ToDegrees(Rotation) < MinRotationAngle)
		{
			_rotatingBack = false;
		}

		if (_rotatingBack) Rotation -= rotationAmount;
		else Rotation += rotationAmount;
	}

	private void Resize()
	{
		var scaleAmount = _scalingSpeedVector * (float) BlastiaGame.GameTimeElapsedSeconds;
		if (Scale.BiggerThanFloat(MaxScale))
		{
			_scalingBack = true;
		}
		else if (Scale.SmallerThanFloat(MinScale))
		{
			_scalingBack = false;
		}
		
		if(_scalingBack) Scale -= scaleAmount;
		else Scale += scaleAmount;
	}
}