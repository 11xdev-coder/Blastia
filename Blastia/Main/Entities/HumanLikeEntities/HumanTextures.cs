using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities.HumanLikeEntities;

public struct HumanTextures(Texture2D head, Texture2D body, Texture2D leftArm, Texture2D rightArm, Texture2D leg)
{
    public Texture2D Head = head;
    public Texture2D Body = body;
    public Texture2D LeftArm = leftArm;
    public Texture2D RightArm = rightArm;
    public Texture2D Leg = leg;
}