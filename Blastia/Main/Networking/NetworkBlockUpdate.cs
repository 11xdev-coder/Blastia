using Microsoft.Xna.Framework;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkBlockUpdate 
{
    public Vector2 Position { get; set; }
    public TileLayer Layer { get; set; }
        
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Position.X);
        writer.Write(Position.Y);
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
            Layer = (TileLayer) reader.ReadByte(),
        };
    }
}

[Serializable]
public class NetworkBlockUpdateAtPositions
{
    public List<Vector2> Positions { get; set; } = [];
        
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Positions.Count);
        foreach (var position in Positions) 
        {
            writer.Write(position.X);
            writer.Write(position.Y);
        }

        return stream.ToArray();
    }
    
    public static NetworkBlockUpdateAtPositions Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var result = new NetworkBlockUpdateAtPositions();
        var count = reader.ReadInt32();
        
        for (int i = 0; i < count; i++) 
        {
            var position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            result.Positions.Add(position);
        }

        return result;
    }
}