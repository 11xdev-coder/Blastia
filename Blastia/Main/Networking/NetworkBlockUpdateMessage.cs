using Microsoft.Xna.Framework;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkBlockUpdateMessage 
{
    /// <summary>
    /// Position before updating the block
    /// </summary>
    public Vector2 OriginalPosition { get; set; }
    /// <summary>
    /// Position after updating the block
    /// </summary>
    public Vector2 NewPosition { get; set; }
    public float Damage { get; set; }
    public TileLayer Layer { get; set; }
        
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(OriginalPosition.X);
        writer.Write(OriginalPosition.Y);
        writer.Write(NewPosition.X);
        writer.Write(NewPosition.Y);
        writer.Write(Damage);
        writer.Write((byte)Layer);

        return stream.ToArray();
    }
    
    public static NetworkBlockUpdateMessage Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        return new NetworkBlockUpdateMessage
        {
            OriginalPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            NewPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Damage = reader.ReadSingle(),
            Layer = (TileLayer) reader.ReadByte(),
        };
    }
}