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
        
        if (entity is Player player)
        {
            player.SteamId = SteamId;
            player.Name = Name;
            player.SelectedHotbarSlot = SelectedSlot;
        }
    }

    public byte[] Serialize(MemoryStream stream, BinaryWriter writer)
    {
        stream.SetLength(0);
        stream.Position = 0;
    
        // entity
        writer.Write(Id);
        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(DirectionVector.X);
        writer.Write(DirectionVector.Y);
        writer.Write(MovementVector.X);
        writer.Write(MovementVector.Y);
        writer.Write(Life);
        writer.Write(IsGrounded);
        writer.Write(CanJump);
        writer.Write(SpriteDirection);
        writer.Write(NetworkTimestamp);
    
        // player
        writer.Write(SteamId.m_SteamID);
        writer.Write(Name);
        writer.Write(SelectedSlot);

        return stream.ToArray();
    }

    public new static NetworkPlayer Deserialize(BinaryReader reader)
    {
        return new NetworkPlayer
        {
            // entity
            Id = reader.ReadUInt16(),
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            DirectionVector = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            MovementVector = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Life = reader.ReadSingle(),
            IsGrounded = reader.ReadBoolean(),
            CanJump = reader.ReadBoolean(),
            SpriteDirection = reader.ReadSingle(),
            NetworkTimestamp = reader.ReadSingle(),
        
            // player
            SteamId = new CSteamID(reader.ReadUInt64()),
            Name = reader.ReadString(),
            SelectedSlot = reader.ReadInt32()
        };
    }
}