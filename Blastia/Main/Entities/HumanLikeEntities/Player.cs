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

	protected override bool FlipSpriteHorizontallyOnKeyPress => true;

	private const float ArmMaxAngle = 20;
	private const float LegMaxAngle = 25;
	private const float WalkingAnimationDuration = 0.4f;
	private const float JumpHeight = 200f;

	public Camera? Camera { get; set; }

	protected override bool ApplyGravity => true;
	protected override int Height => 2;
	protected override int Width => 1;
	protected override float Mass => 46f;
	
	private const float MinJumpVelocity = 150f;
	private const float MaxJumpVelocity = 375f;
	private const float MaxChargeTime = 0.6f;
	private Vector2 _walkingVector;
	private float _jumpCharge;

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
				DrawWidth = 240 * Block.Size,
				DrawHeight = 135 * Block.Size
			};
		}
		MovementSpeed = 3f;
	}

	public override void Update()
	{
		if (IsPreview) PreviewUpdate();
		else RegularUpdate();
		
		base.Update();
		
		if (!IsPreview) UpdateCamera();
	}

	/// <summary>
	/// Update when IsPreview = false
	/// </summary>
	private void RegularUpdate()
	{
		if (LocallyControlled)
		{
			HandleMovement();
		}
	}

	private void UpdateCamera()
	{
		Camera?.Update();
		MakeCameraFollow();
	}

	private void HandleMovement()
	{
		_walkingVector = Vector2.Zero;
		
		Vector2 directionVector = Vector2.Zero;
		KeyboardHelper.AccumulateValueFromMap(HorizontalMovementMap, ref directionVector);

		// less speed when in air
		var airMultiplier = 1f;
		if (!IsGrounded) airMultiplier = 0.4f;
		
		if (directionVector != Vector2.Zero)
		{
			WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
			
			directionVector = Vector2Extensions.Normalize(directionVector);
			_walkingVector = directionVector * MovementSpeed * airMultiplier;
			MovementVector += _walkingVector;
		}
		else
		{
			StopAnimations();
		}
		
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.Space) && IsGrounded)
		{
			_jumpCharge += (float) BlastiaGame.GameTimeElapsedSeconds;
		}

		else if (BlastiaGame.KeyboardState.IsKeyUp(Keys.Space) &&
		         BlastiaGame.PreviousKeyboardState.IsKeyDown(Keys.Space) && IsGrounded)
		{
			_jumpCharge = Math.Min(_jumpCharge, MaxChargeTime);
			float chargeRatio = _jumpCharge / MaxChargeTime;
			var boostedJump = MathHelper.Lerp(MinJumpVelocity, MaxJumpVelocity, chargeRatio);
			
			var jumpHeight = boostedJump;
			MovementVector.Y = -jumpHeight;
			_jumpCharge = 0;
		}
	}

	private void MakeCameraFollow()
	{
		if (Camera == null)
			return;

		float effectiveViewWidth = Camera.DrawWidth / Camera.CameraScale;
		float effectiveViewHeight = Camera.DrawHeight / Camera.CameraScale;

		Camera.Position = new Vector2(
			Position.X - effectiveViewWidth * 0.5f,
			Position.Y - effectiveViewHeight * 0.5f
		);
	}

	/// <summary>
	/// Update called when IsPreview = true
	/// </summary>
	private void PreviewUpdate()
	{
		WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
	}
}