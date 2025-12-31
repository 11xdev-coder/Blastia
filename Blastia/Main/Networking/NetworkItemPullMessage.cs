namespace Blastia.Main.Networking;

[Serializable]
public class NetworkItemPullMessage 
{
    public Guid DroppedItemNetworkId;
    public ulong PullerId;
    public bool IsPulling;
    
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(DroppedItemNetworkId.ToByteArray());
        writer.Write(PullerId);
        writer.Write(IsPulling);

        return stream.ToArray();
    }
    
    public static NetworkItemPullMessage Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var guidBytes = reader.ReadBytes(16);
        var networkId = new Guid(guidBytes);

        return new NetworkItemPullMessage
        {
            DroppedItemNetworkId = networkId,
            PullerId = reader.ReadUInt64(),
            IsPulling = reader.ReadBoolean()
        };
    }
}