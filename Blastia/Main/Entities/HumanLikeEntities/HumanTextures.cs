using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities.HumanLikeEntities;

public struct HumanTextures
{
    public Texture2D Head;
    public Texture2D Body;
    public Texture2D LeftArm;
    public Texture2D RightArm;
    public Texture2D Leg;

    public HumanTextures(Texture2D head, Texture2D body, Texture2D leftArm, Texture2D rightArm, Texture2D leg)
    {
        Head = head;
        Body = body;
        LeftArm = leftArm;
        RightArm = rightArm;
        Leg = leg;
    }
}