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
        if (entity.LocallyControlled) return;
        
        entity.SetId(Id);
        
        entity.NetworkPosition = Position;
        entity.NetworkMovementVector = MovementVector;
        entity.DirectionVector = DirectionVector;
        entity.Life = Life;
        entity.IsGrounded = IsGrounded;
        entity.CanJump = CanJump;
        entity.SpriteDirection = SpriteDirection;
        
        entity.NetworkTimestamp = NetworkTimestamp;
    }

    public virtual byte[] Serialize(MemoryStream stream, BinaryWriter writer)
    {
        stream.SetLength(0);
        stream.Position = 0;
        
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
        
        return stream.ToArray();
    }

    public static NetworkEntity Deserialize(BinaryReader reader)
    {
        return new NetworkEntity
        {
            Id = reader.ReadUInt16(),
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