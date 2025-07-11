using Microsoft.Xna.Framework;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkBlockUpdate 
{
    public Vector2 Position { get; set; }
    public float Damage { get; set; }
    public TileLayer Layer { get; set; }
        
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Damage);
        writer.Write((byte)Layer);

        return stream.ToArray();
    }
    
    public static NetworkBlockUpdate Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        return new NetworkBlockUpdate
        {
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Damage = reader.ReadSingle(),
            Layer = (TileLayer) reader.ReadByte(),
        };
    }
}