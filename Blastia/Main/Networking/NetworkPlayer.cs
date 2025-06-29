using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
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
        var baseData = base.Serialize(stream, writer);
        writer.Write(baseData.Length);
        writer.Write(baseData);
        
        writer.Write(SteamId.m_SteamID);
        writer.Write(Name);
        writer.Write(SelectedSlot);

        return stream.ToArray();
    }

    public override NetworkPlayer Deserialize(BinaryReader reader)
    {
        var baseDataLength = reader.ReadInt32();
        var baseData = reader.ReadBytes(baseDataLength);
        
        using var baseStream = new MemoryStream(baseData);
        using var baseReader = new BinaryReader(baseStream);
        var baseEntity = base.Deserialize(baseReader);
        
        var networkPlayer = new NetworkPlayer
        {
            Id = baseEntity.Id,
            Position = baseEntity.Position,
            MovementVector = baseEntity.MovementVector,
            Life = baseEntity.Life,
            IsGrounded = baseEntity.IsGrounded,
            CanJump = baseEntity.CanJump,
            NetworkTimestamp = baseEntity.NetworkTimestamp,
            SteamId = new CSteamID(reader.ReadUInt64()),
            Name = reader.ReadString(),
            SelectedSlot = reader.ReadInt32()
        };
        return networkPlayer;
    }
}