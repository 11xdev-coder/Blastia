using Microsoft.Xna.Framework;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkSignEditedMessage 
{
    public Vector2 Position;
    public string NewText = "";
    
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(NewText);

        return stream.ToArray();
    }
    
    public static NetworkSignEditedMessage Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        return new NetworkSignEditedMessage
        {
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            NewText = reader.ReadString()
        };
    }
}