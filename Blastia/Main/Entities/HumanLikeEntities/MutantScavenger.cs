using Blastia.Main.Entities.Common;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Entities.HumanLikeEntities;

/// <summary>
/// Zombie enemy that will as the player progresses acquire new gear (armor, weapons, etc.) and learn how to work in groups
/// (maybe some archers stand in the back while warriors with swords rush to the player). At the start of the game will
/// walk with bare hands.
/// </summary>
[Entity(Id = EntityID.MutantScavenger)]
public class MutantScavenger : HumanLikeEntity
{
    public MutantScavenger(Vector2 position, float scale = 0.17f) : base(position, scale, EntityID.MutantScavenger,
        new Vector2(0, -24), Vector2.Zero, new Vector2(-13, -21), 
        new Vector2(13, -21), new Vector2(-6, 21), new Vector2(10, 21))
    {
    }
}