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
	
	private const float MinJumpVelocity = 200f;
	private const float MaxJumpVelocity = 320f;
	private const float MaxChargeTime = 0.35f;
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
		MovementSpeed = 80f;
		AccelerationFactor = 10f;
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
			HandleMouseClicks();
		}
	}

	private void UpdateCamera()
	{
		Camera?.Update();
		MakeCameraFollow();
	}

	private void HandleMovement()
	{
		Vector2 directionVector = Vector2.Zero;
		KeyboardHelper.AccumulateValueFromMap(HorizontalMovementMap, ref directionVector);

		// less speed when in air
		var airMultiplier = 1f;
		if (!IsGrounded) airMultiplier = 0.4f;

		var targetHorizontalSpeed = 0f;
		if (directionVector != Vector2.Zero)
		{
			WalkingAnimation(ArmMaxAngle, LegMaxAngle, WalkingAnimationDuration);
			targetHorizontalSpeed = directionVector.X * MovementSpeed * airMultiplier;
		}
		else
		{
			StopAnimations();
		}
		
		MovementVector.X = MathHelper.Lerp(MovementVector.X, targetHorizontalSpeed, 
			AccelerationFactor * (float) BlastiaGame.GameTimeElapsedSeconds);
		
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.Space) && CanJump)
		{
			_jumpCharge += (float) BlastiaGame.GameTimeElapsedSeconds;
		}
		else if (BlastiaGame.KeyboardState.IsKeyUp(Keys.Space) &&
		         BlastiaGame.PreviousKeyboardState.IsKeyDown(Keys.Space) && CanJump)
		{
			_jumpCharge = Math.Min(_jumpCharge, MaxChargeTime);
			float chargeRatio = _jumpCharge / MaxChargeTime;
			var boostedJump = MathHelper.Lerp(MinJumpVelocity, MaxJumpVelocity, chargeRatio);
			
			var jumpHeight = boostedJump;
			MovementVector.Y = -jumpHeight;
			_jumpCharge = 0;
		}
	}

	private Vector2 GetCoordsForBlockPlacement()
	{
		if (Camera == null) return Vector2.Zero;
		
		var worldPos = Camera.ScreenToWorld(BlastiaGame.CursorPosition);
		var posX = (int) Math.Floor(worldPos.X / Block.Size) * Block.Size;
		var posY = (int) Math.Floor(worldPos.Y / Block.Size) * Block.Size;
		
		return new Vector2(posX, posY);
	}
	
	private void HandleMouseClicks()
	{
		var currentWorld = PlayerManager.Instance.SelectedWorld;
		if (currentWorld == null) return;
			
		if (BlastiaGame.HasClickedRight)
		{
			var pos = GetCoordsForBlockPlacement();
			currentWorld.SetTile((int) pos.X, (int) pos.Y, 1);
		}
		if (BlastiaGame.HasClickedLeft)
		{
			var pos = GetCoordsForBlockPlacement();
			currentWorld.SetTile((int) pos.X, (int) pos.Y, 0);
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