using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.GameState;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.Entities.HumanLikeEntities;

[Entity(Id = EntityID.Player)]
public class Player : HumanLikeEntity
{
	/// <summary>
	/// If True, player will play walking animation and disable all other logic
	/// </summary>
	public bool IsPreview { get; set; }
	public bool LocallyControlled { get; private set; }

	private const float ArmMaxAngle = 20;
	private const float LegMaxAngle = 25;
	private const float WalkingAnimationDuration = 0.4f;

	public Camera? Camera { get; set; }

	protected override bool ApplyGravity => true;
	protected override int Height => 2;
	protected override int Width => 1;
	protected override float Mass => 46f;
	
	private Vector2 _walkingVector;

	public Player(Vector2 position, float initialScaleFactor = 1f, bool myPlayer = false) : 
		base(position, initialScaleFactor, EntityID.Player, new Vector2(0, -24), Vector2.Zero, 
			new Vector2(-13, -21), new Vector2(13, -21), new Vector2(-6, 21), 
			new Vector2(10, 21))
	{
		LocallyControlled = myPlayer;
		
		if (myPlayer)
		{
			Camera = new Camera(position)
			{
				DrawWidth = (int) BlastiaGame.ScreenWidth,
				DrawHeight = (int) BlastiaGame.ScreenHeight
			};
		}
		MovementSpeed = 20f;
	}

	public override void Update()
	{
		if (IsPreview) PreviewUpdate();
		else RegularUpdate();
		
		base.Update();
	}

	/// <summary>
	/// Update when IsPreview = false
	/// </summary>
	private void RegularUpdate()
	{
		if (LocallyControlled)
		{
			HandleMovement();
			
			Camera?.Update();
			MakeCameraFollow();
		}
	}

	private void HandleMovement()
	{
		_walkingVector = Vector2.Zero;
		
		Vector2 directionVector = Vector2.Zero;
		KeyboardHelper.AccumulateValueFromMap(HorizontalMovementMap, ref directionVector);
		
		if (directionVector != Vector2.Zero)
			directionVector = Vector2Extensions.Normalize(directionVector);
		
		_walkingVector = directionVector * MovementSpeed;
		MovementVector += _walkingVector;

		// jump
		if (KeyboardHelper.IsKeyJustPressed(Keys.Space))
		{
			AddImpulse(new Vector2(0, -5), 0.2f);
		}
	}

	private void MakeCameraFollow()
	{
		if (Camera == null) return;

		var matrix = VideoManager.Instance.CalculateResolutionScaleMatrix();
		float x = Position.X - (BlastiaGame.ScreenWidth * 0.5f / Camera.CameraScale) / matrix.M11;
		float y = Position.Y - (BlastiaGame.ScreenHeight * 0.5f / Camera.CameraScale) / matrix.M22;
		Camera.Position = new Vector2(x, y);
	}

	/// <summary>
	/// Update called when IsPreview = true
	/// </summary>
	private void PreviewUpdate()
	{
		WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
	}
}