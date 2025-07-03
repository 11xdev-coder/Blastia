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

    public new static NetworkPlayer Deserialize(BinaryReader reader)
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

[Serializable]
public class PlayerInputState
{
    public Vector2 MovementInput { get; set; }
    public bool Jump { get; set; }
    public bool IsMoving { get; set; }
    public float JumpCharge { get; set; }
    public float Timestamp { get; set; }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        writer.Write(MovementInput.X);
        writer.Write(MovementInput.Y);
        writer.Write(Jump);
        writer.Write(IsMoving);
        writer.Write(JumpCharge);
        writer.Write(Timestamp);

        return stream.ToArray();
    }

    public static PlayerInputState Deserialize(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);

        return new PlayerInputState
        {
            MovementInput = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Jump = reader.ReadBoolean(),
            IsMoving = reader.ReadBoolean(),
            JumpCharge = reader.ReadSingle(),
            Timestamp = reader.ReadSingle()
        };
    }
}