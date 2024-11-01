using Blastia.Main.Entities.Common;
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

	protected override void SetDefaults()
	{
		base.SetDefaults();

		var textures = StuffRegistry.GetHumanTextures(ID);
		if (textures == null) return;
		
		var head = textures.Value.Head;
		var body = textures.Value.Body;
		var leftArm = textures.Value.LeftArm;
		var rightArm = textures.Value.RightArm;
		var leg = textures.Value.Leg;
		
		// bottom origin
		Head = new BodyPart(head, new Vector2(0, -24), origin: 
			new Vector2(head.Width * 0.5f, head.Height));
		// centered origin
		Body = new BodyPart(body, Vector2.Zero);
		// right-top corner origin
		LeftArm = new BodyPart(leftArm, new Vector2(-13, -21), origin:
			new Vector2(leftArm.Width, 0f));
		// left-top corner origin
		RightArm = new BodyPart(rightArm, new Vector2(13, -21), origin:
			new Vector2(0f, 0f));
		// top origin
		Vector2 topOrigin = new Vector2(leg.Width * 0.5f, 0);
		LeftLeg = new BodyPart(leg, new Vector2(-6, 21), origin: topOrigin);
		RightLeg = new BodyPart(leg, new Vector2(10, 21), origin: topOrigin);
	}

	public override void Update()
    {
        
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