using Microsoft.Xna.Framework;
using Steamworks;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkChatMessage 
{
    public string Text = "";
    public string SenderName = "";
    
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Text);
        writer.Write(SenderName);
        return stream.ToArray();
    }
    
    public static NetworkChatMessage Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        
        return new NetworkChatMessage
        {
            Text = reader.ReadString(),
            SenderName = reader.ReadString()
        };
    }
}