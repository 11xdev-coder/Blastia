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
	protected override float Height => 2;

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
		MovementSpeed = 0.25f;
	}

	public override void Update()
	{
		if (IsPreview) PreviewUpdate();
		else RegularUpdate();
	}

	/// <summary>
	/// Update when IsPreview = false
	/// </summary>
	private void RegularUpdate()
	{
		if (LocallyControlled)
		{
			HandleMovement();
			ApplyGravityForce();
			
			Camera?.Update();
			MakeCameraFollow();
		}
	}

	private void HandleMovement()
	{
		MovementVector = Vector2.Zero;
		
		Vector2 directionVector = Vector2.Zero;
		KeyboardHelper.AccumulateValueFromMap(MovementMap, ref directionVector);
		
		if (directionVector != Vector2.Zero)
			directionVector = Vector2Extensions.Normalize(directionVector);
		
		MovementVector = directionVector * MovementSpeed;
		UpdatePosition();
	}

	private void MakeCameraFollow()
	{
		if (Camera == null) return;

		float x = Position.X - Camera.DrawWidth * 0.5f / Camera.CameraScale;
		float y = Position.Y - Camera.DrawHeight * 0.5f / Camera.CameraScale;
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