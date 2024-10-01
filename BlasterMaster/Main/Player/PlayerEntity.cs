using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Player;

public class PlayerEntity
{
    /// <summary>
    /// If True, player will play walking animation and disable all other logic
    /// </summary>
    public bool IsPreview { get; set; }
    
    public Vector2 Position { get; set; }
    
    #region Bodyparts
    
    public BodyPart Head { get; set; }
    public BodyPart Body { get; set; }
    public BodyPart LeftArm { get; set; }
    public BodyPart RightArm { get; set; }
    public BodyPart LeftLeg { get; set; }
    public BodyPart RightLeg { get; set; }
    
    #endregion

    public PlayerEntity(Vector2 position)
    {
        Position = position;
        
        Head = new BodyPart(BlasterMasterGame.PlayerHead, new Vector2(0, 10));
        Body = new BodyPart(BlasterMasterGame.PlayerBody, new Vector2(0, 5));
        LeftArm = new BodyPart(BlasterMasterGame.PlayerLeftArm, Vector2.Zero);
        RightArm = new BodyPart(BlasterMasterGame.PlayerRightArm, Vector2.Zero);
        LeftLeg = new BodyPart(BlasterMasterGame.PlayerLeg, Vector2.Zero);
        RightLeg = new BodyPart(BlasterMasterGame.PlayerLeg, Vector2.Zero);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Head.Draw(spriteBatch, Position);
        Body.Draw(spriteBatch, Position);
        LeftArm.Draw(spriteBatch, Position);
        RightArm.Draw(spriteBatch, Position);
        LeftLeg.Draw(spriteBatch, Position);
        RightLeg.Draw(spriteBatch, Position);
    }
}