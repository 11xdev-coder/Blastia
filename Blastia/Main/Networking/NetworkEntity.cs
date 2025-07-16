using Blastia.Main.Entities.Common;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Networking;

/// <summary>
/// Lightweight network representation of <c>Entity</c>
/// </summary>
[Serializable]
public class NetworkEntity
{
    public ushort Id { get; set; }
    /// <summary>
    /// Players use steam IDs instead of GUIDs
    /// </summary>
    public Guid NetworkId { get; set; } = Guid.Empty;
    public Vector2 Position { get; set; }
    public Vector2 DirectionVector { get; set; }
    public Vector2 MovementVector { get; set; }
    public float Life { get; set; }
    public bool IsGrounded;
    public bool CanJump;
    public float SpriteDirection { get; set; }
    
    public float NetworkTimestamp { get; set; }

    public virtual void FromEntity(Entity entity)
    {
        Id = entity.GetId();
        NetworkId = entity.NetworkId;
        
        Position = entity.Position;
        DirectionVector = entity.DirectionVector;
        MovementVector = entity.MovementVector;
        Life = entity.Life;
        IsGrounded = entity.IsGrounded;
        CanJump = entity.CanJump;
        SpriteDirection = entity.SpriteDirection;
        
        // for smooth interpolation
        NetworkTimestamp = (float) DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// Changes basic entity's properties to this <c>NetworkEntity</c>'s values
    /// </summary>
    /// <param name="entity">Entity to apply values to</param>
    public virtual void ApplyToEntity(Entity entity)
    {
        entity.SetId(Id);
        entity.NetworkId = NetworkId;

        entity.NetworkPosition = Position;
        entity.NetworkMovementVector = MovementVector;
        entity.DirectionVector = DirectionVector;
        entity.Life = Life;
        entity.IsGrounded = IsGrounded;
        entity.CanJump = CanJump;
        entity.SpriteDirection = SpriteDirection;
        
        entity.NetworkTimestamp = NetworkTimestamp;
    }

    public virtual byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        writer.Write(Id);
        writer.Write(NetworkId.ToByteArray());
        
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
        
        return stream.ToArray();
    }

    public static NetworkEntity Deserialize(BinaryReader reader)
    {
        var id = reader.ReadUInt16();

        var guidBytes = reader.ReadBytes(16);
        var networkId = new Guid(guidBytes);
        
        return new NetworkEntity
        {
            Id = id,
            NetworkId = networkId,
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            DirectionVector = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            MovementVector = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Life = reader.ReadSingle(),
            IsGrounded = reader.ReadBoolean(),
            CanJump = reader.ReadBoolean(),
            SpriteDirection = reader.ReadSingle(),
            NetworkTimestamp = reader.ReadSingle()
        };
    }
}