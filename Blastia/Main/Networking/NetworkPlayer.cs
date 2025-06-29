using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Microsoft.Xna.Framework;
using Steamworks;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkPlayer : NetworkEntity
{
    public CSteamID SteamId;
    public string Name = "";
    public int SelectedSlot;

    public override void FromEntity(Entity entity)
    {
        base.FromEntity(entity);

        if (entity is Player player)
        {
            SteamId = player.SteamId;
            Name = player.Name;
            SelectedSlot = player.SelectedHotbarSlot;
        }
    }

    public override void ApplyToEntity(Entity entity)
    {
        base.ApplyToEntity(entity);
        
        if (entity is Player player && entity.LocallyControlled)
        {
            player.SteamId = SteamId;
            player.Name = Name;
            player.SelectedHotbarSlot = SelectedSlot;
        }
    }

    public override byte[] Serialize(MemoryStream stream, BinaryWriter writer)
    {
        stream.SetLength(0);
        stream.Position = 0;
    
        writer.Write(Id);
        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(MovementVector.X);
        writer.Write(MovementVector.Y);
        writer.Write(Life);
        writer.Write(IsGrounded);
        writer.Write(CanJump);
        writer.Write(NetworkTimestamp);
    
        writer.Write(SteamId.m_SteamID);
        writer.Write(Name);
        writer.Write(SelectedSlot);

        return stream.ToArray();
    }

    public override NetworkPlayer Deserialize(BinaryReader reader)
    {
        return new NetworkPlayer
        {
            // Read base entity data
            Id = reader.ReadUInt16(),
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            MovementVector = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Life = reader.ReadSingle(),
            IsGrounded = reader.ReadBoolean(),
            CanJump = reader.ReadBoolean(),
            NetworkTimestamp = reader.ReadSingle(),
        
            // Read player-specific data
            SteamId = new CSteamID(reader.ReadUInt64()),
            Name = reader.ReadString(),
            SelectedSlot = reader.ReadInt32()
        };
    }
}