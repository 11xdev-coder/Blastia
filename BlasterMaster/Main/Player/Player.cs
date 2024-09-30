using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Player;

public class Player
{
    /// <summary>
    /// If True, player will play walking animation and disable all other logic
    /// </summary>
    public bool IsPreview { get; set; }
    
    #region Bodyparts
    
    public BodyPart Head { get; set; }
    public BodyPart Body { get; set; }
    public BodyPart LeftArm { get; set; }
    public BodyPart RightArm { get; set; }
    public BodyPart LeftLeg { get; set; }
    public BodyPart RightLeg { get; set; }
    
    #endregion

    public Player()
    {
        Head = new BodyPart(BlasterMasterGame.PlayerHead, new Vector2(0, 10));
        Body = new BodyPart(BlasterMasterGame.PlayerBody, Vector2.Zero);
        LeftArm = new BodyPart(BlasterMasterGame.PlayerLeftArm, Vector2.Zero);
        RightArm = new BodyPart(BlasterMasterGame.PlayerRightArm, Vector2.Zero);
        LeftLeg = new BodyPart(BlasterMasterGame.PlayerLeg, Vector2.Zero);
        RightLeg = new BodyPart(BlasterMasterGame.PlayerLeg, Vector2.Zero);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Head.Draw(spriteBatch);
        Body.Draw(spriteBatch);
        LeftArm.Draw(spriteBatch);
        RightArm.Draw(spriteBatch);
        LeftLeg.Draw(spriteBatch);
        RightLeg.Draw(spriteBatch);
    }
}