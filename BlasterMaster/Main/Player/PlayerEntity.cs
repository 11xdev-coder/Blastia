using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Player;

public class PlayerEntity
{
    /// <summary>
    /// If True, player will play walking animation and disable all other logic
    /// </summary>
    public bool IsPreview { get; set; }
    private double _animationTimeElapsed;

    public float ArmMaxAngle = 20;
    public float LegMaxAngle = 25;
    public float WalkingAnimationDuration = 0.4f;
    
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
        
        // origin at the bottom
        Head = new BodyPart(BlasterMasterGame.PlayerTextures.PlayerHead, new Vector2(0, -25), origin: 
            new Vector2(BlasterMasterGame.PlayerTextures.PlayerHead.Width * 0.5f, BlasterMasterGame.PlayerTextures.PlayerHead.Height));
        // centered origin
        Body = new BodyPart(BlasterMasterGame.PlayerTextures.PlayerBody, Vector2.Zero);
        // right-top corner origin
        LeftArm = new BodyPart(BlasterMasterGame.PlayerTextures.PlayerLeftArm, new Vector2(-14, -22), origin:
            new Vector2(BlasterMasterGame.PlayerTextures.PlayerLeftArm.Width, 0f));
        // left-top corner origin
        RightArm = new BodyPart(BlasterMasterGame.PlayerTextures.PlayerRightArm, new Vector2(13, -22), origin:
            new Vector2(0f, 0f));
        // top origin
        Vector2 topOrigin = new Vector2(BlasterMasterGame.PlayerTextures.PlayerLeg.Width * 0.5f, 0);
        LeftLeg = new BodyPart(BlasterMasterGame.PlayerTextures.PlayerLeg, new Vector2(-7, 21), origin: topOrigin);
        RightLeg = new BodyPart(BlasterMasterGame.PlayerTextures.PlayerLeg, new Vector2(11, 21), origin: topOrigin);
    }

    public void Update()
    {
        if(IsPreview) PreviewUpdate();
        else RegularUpdate();
    }

    /// <summary>
    /// Update when IsPreview = false
    /// </summary>
    private void RegularUpdate()
    {
        
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
        _animationTimeElapsed += BlasterMasterGame.GameTimeElapsedSeconds;
        
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

    public void Draw(SpriteBatch spriteBatch)
    {
        Head.Draw(spriteBatch, Position);
        
        RightArm.Draw(spriteBatch, Position); // right arm behind Body
        Body.Draw(spriteBatch, Position);
        
        LeftArm.Draw(spriteBatch, Position);
        LeftLeg.Draw(spriteBatch, Position);
        RightLeg.Draw(spriteBatch, Position);
    }
}