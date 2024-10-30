using Blastia.Main.GameState;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.Entities;

public class Player : Entity
{
	/// <summary>
	/// If True, player will play walking animation and disable all other logic
	/// </summary>
	public bool IsPreview { get; set; }
	private double _animationTimeElapsed;

	public float ArmMaxAngle = 20;
	public float LegMaxAngle = 25;
	public float WalkingAnimationDuration = 0.4f;
	
	// BODYPARTS
	public BodyPart Head { get; set; }
	public BodyPart Body { get; set; }
	public BodyPart LeftArm { get; set; }
	public BodyPart RightArm { get; set; }
	public BodyPart LeftLeg { get; set; }
	public BodyPart RightLeg { get; set; }
	
	public Camera Camera { get; set; }

	public Player(Vector2 position)
	{
		Position = position;
		Camera = new Camera(position)
		{
			DrawWidth = (int) BlastiaGame.ScreenWidth,
			DrawHeight = (int) BlastiaGame.ScreenHeight
		};
		
		// player textures shortcuts
		var playerHead = BlastiaGame.PlayerTextures.PlayerHead;
		var playerBody = BlastiaGame.PlayerTextures.PlayerBody;
		var playerLeftArm = BlastiaGame.PlayerTextures.PlayerLeftArm;
		var playerRightArm = BlastiaGame.PlayerTextures.PlayerRightArm;
		var playerLeg = BlastiaGame.PlayerTextures.PlayerLeg;
		
		// origin at the bottom
		Head = new BodyPart(playerHead, new Vector2(0, -25), origin: 
			new Vector2(playerHead.Width * 0.5f, playerHead.Height));
		// centered origin
		Body = new BodyPart(playerBody, Vector2.Zero);
		// right-top corner origin
		LeftArm = new BodyPart(playerLeftArm, new Vector2(-14, -22), origin:
			new Vector2(playerLeftArm.Width, 0f));
		// left-top corner origin
		RightArm = new BodyPart(playerRightArm, new Vector2(13, -22), origin:
			new Vector2(0f, 0f));
		// top origin
		Vector2 topOrigin = new Vector2(playerLeg.Width * 0.5f, 0);
		LeftLeg = new BodyPart(playerLeg, new Vector2(-7, 21), origin: topOrigin);
		RightLeg = new BodyPart(playerLeg, new Vector2(11, 21), origin: topOrigin);
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
		// movement
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.A))
		{
			Position.X -= 0.25f;
		}
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.S))
		{
			Position.Y += 0.25f;
		}
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.D))
		{
			Position.X += 0.25f;
		}
		if (BlastiaGame.KeyboardState.IsKeyDown(Keys.W))
		{
			Position.Y -= 0.25f;
		}
		
		Camera.Update();
	}

	/// <summary>
	/// Update called when IsPreview = true
	/// </summary>
	private void PreviewUpdate()
	{
		WalkingAnimation();
	}

	/// <summary>
	/// Rotates body parts creating walking animation
	/// </summary>
	private void WalkingAnimation()
	{
		_animationTimeElapsed += BlastiaGame.GameTimeElapsedSeconds;
		
		// left arm
		LeftArm.Rotation = MathUtilities.PingPongLerpRadians(-ArmMaxAngle, ArmMaxAngle, 
			(float) _animationTimeElapsed, WalkingAnimationDuration);
		
		// right arm
		RightArm.Rotation = MathUtilities.PingPongLerpRadians(ArmMaxAngle, -ArmMaxAngle, 
			(float) _animationTimeElapsed, WalkingAnimationDuration);
		
		// left leg
		LeftLeg.Rotation = MathUtilities.PingPongLerpRadians(-LegMaxAngle, LegMaxAngle, 
			(float) _animationTimeElapsed, WalkingAnimationDuration);
		
		// right leg
		RightLeg.Rotation = MathUtilities.PingPongLerpRadians(LegMaxAngle, -LegMaxAngle, 
			(float) _animationTimeElapsed, WalkingAnimationDuration);
	}

	/// <summary>
	/// Draws BodyParts at player position
	/// </summary>
	/// <param name="spriteBatch"></param>
	public override void Draw(SpriteBatch spriteBatch)
	{
		Head.Draw(spriteBatch, Position);
		
		RightArm.Draw(spriteBatch, Position); // right arm behind Body
		Body.Draw(spriteBatch, Position);
		
		LeftArm.Draw(spriteBatch, Position);
		LeftLeg.Draw(spriteBatch, Position);
		RightLeg.Draw(spriteBatch, Position);
	}
	
	/// <summary>
	/// Draws player BodyParts with a specified position and scale
	/// </summary>
	/// <param name="spriteBatch"></param>
	/// <param name="position">Where to draw the BodyParts</param>
	/// <param name="scale">BodyParts scale</param>
	public void Draw(SpriteBatch spriteBatch, Vector2 position, float scale = 1f)
	{
		Head.Draw(spriteBatch, position, scale);
		
		RightArm.Draw(spriteBatch, position, scale); // right arm behind Body
		Body.Draw(spriteBatch, position, scale);
		
		LeftArm.Draw(spriteBatch, position, scale);
		LeftLeg.Draw(spriteBatch, position, scale);
		RightLeg.Draw(spriteBatch, position, scale);
	}
}