using Blastia.Main.Entities.Common;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities.HumanLikeEntities;

public abstract class HumanLikeEntity : Entity
{
	public BodyPart Head { get; set; }
	public BodyPart Body { get; set; }
	public BodyPart LeftArm { get; set; }
	public BodyPart RightArm { get; set; }
	public BodyPart LeftLeg { get; set; }
	public BodyPart RightLeg { get; set; }

	private double _animationTimeElapsed;

	protected HumanLikeEntity(Vector2 headOffset, Vector2 bodyOffset, Vector2 leftArmOffset, Vector2 rightArmOffset, 
		Vector2 leftLegOffset, Vector2 rightLegOffset)
	{
		var textures = StuffRegistry.GetHumanTextures(GetId());
		if (textures == null) throw new NullReferenceException($"No textures found for entity with ID: {GetId()}");
		
		var head = textures.Value.Head;
		var body = textures.Value.Body;
		var leftArm = textures.Value.LeftArm;
		var rightArm = textures.Value.RightArm;
		var leg = textures.Value.Leg;
		
		// bottom origin
		Head = new BodyPart(head, headOffset, 0f, 
			new Vector2(head.Width * 0.5f, head.Height));
		// centered origin
		Body = new BodyPart(body, bodyOffset);
		// right-top corner origin
		LeftArm = new BodyPart(leftArm, leftArmOffset, 0f,
			new Vector2(leftArm.Width, 0f));
		// left-top corner origin
		RightArm = new BodyPart(rightArm, rightArmOffset, 0f,
			new Vector2(0f, 0f));
		// top origin
		Vector2 topOrigin = new Vector2(leg.Width * 0.5f, 0);
		LeftLeg = new BodyPart(leg, leftLegOffset, 0f, topOrigin);
		RightLeg = new BodyPart(leg, rightLegOffset, 0f, topOrigin);
	}

	public override void Update()
    {
        
    }
	
	/// <summary>
	/// Rotates arms and legs overtime with specified duration
	/// </summary>
	/// <param name="armMaxAngle">Max rotating angle of arms</param>
	/// <param name="legMaxAngle">Max rotating angle of legs</param>
	/// <param name="duration">Shorter duration -> faster animation</param>
	protected void WalkingAnimation(float armMaxAngle, float legMaxAngle, float duration)
	{
		_animationTimeElapsed += BlastiaGame.GameTimeElapsedSeconds;
		
		// left arm
		LeftArm.Rotation = MathUtilities.PingPongLerpRadians(-armMaxAngle, armMaxAngle, 
			(float) _animationTimeElapsed, duration);
		
		// right arm
		RightArm.Rotation = MathUtilities.PingPongLerpRadians(armMaxAngle, -armMaxAngle, 
			(float) _animationTimeElapsed, duration);
		
		// left leg
		LeftLeg.Rotation = MathUtilities.PingPongLerpRadians(-legMaxAngle, legMaxAngle, 
			(float) _animationTimeElapsed, duration);
		
		// right leg
		RightLeg.Rotation = MathUtilities.PingPongLerpRadians(legMaxAngle, -legMaxAngle, 
			(float) _animationTimeElapsed, duration);
	}

    /// <summary>
    /// Draws BodyParts at player position
    /// </summary>
    /// <param name="spriteBatch"></param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        Draw(spriteBatch, Position);
    }
	
    /// <summary>
    /// Draws player BodyParts with a specified position and scale
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="position">Where to draw the BodyParts</param>
    /// <param name="bodyPartScale">BodyParts scale</param>
    public void Draw(SpriteBatch spriteBatch, Vector2 position, float bodyPartScale = 1f)
    {
        var scaledBodyPartScale = bodyPartScale * Scale;
		
        Head.Draw(spriteBatch, position, scaledBodyPartScale);
		
        RightArm.Draw(spriteBatch, position, scaledBodyPartScale); // right arm behind Body
        Body.Draw(spriteBatch, position, scaledBodyPartScale);
		
        LeftArm.Draw(spriteBatch, position, scaledBodyPartScale);
        LeftLeg.Draw(spriteBatch, position, scaledBodyPartScale);
        RightLeg.Draw(spriteBatch, position, scaledBodyPartScale);
    }
}