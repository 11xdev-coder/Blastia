using Microsoft.Xna.Framework;
using Steamworks;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkChatMessage 
{
    public string Text = "";
    public string? SenderName = "";
    
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Text);
        // have a sender name
        writer.Write(SenderName != null);
        if (SenderName != null) writer.Write(SenderName);
        return stream.ToArray();
    }
    
    public static NetworkChatMessage Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // read values
        var text = reader.ReadString();
        var hasSenderName = reader.ReadBoolean();
        var senderName = "";
        if (hasSenderName) senderName = reader.ReadString();
        
        return new NetworkChatMessage
        {
            Text = text,
            SenderName = senderName
        };
    }
}