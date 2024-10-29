using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class LogoImageElement : Image
{
	private bool _rotatingBack;
	private bool _scalingBack;
	
	private const float RotationSpeed = 0.0005f;
	private const float MaxRotationAngle = 20f;
	private const float MinRotationAngle = -20f;

	private const float ScalingSpeed = 0.0005f;
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
		if (MathHelper.ToDegrees(Rotation) > MaxRotationAngle)
		{
			_rotatingBack = true;
		}
		else if (MathHelper.ToDegrees(Rotation) < MinRotationAngle)
		{
			_rotatingBack = false;
		}

		if (_rotatingBack) Rotation -= RotationSpeed;
		else Rotation += RotationSpeed;
	}

	private void Resize()
	{
		if (Scale.BiggerThanFloat(MaxScale))
		{
			_scalingBack = true;
		}
		else if (Scale.SmallerThanFloat(MinScale))
		{
			_scalingBack = false;
		}
		
		if(_scalingBack) Scale -= _scalingSpeedVector;
		else Scale += _scalingSpeedVector;
	}
}