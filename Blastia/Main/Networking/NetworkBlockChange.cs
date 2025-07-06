using Microsoft.Xna.Framework;
using Steamworks;

namespace Blastia.Main.Networking;

[Serializable]
public class NetworkBlockChange 
{
    public Vector2 Position { get; set; }
    public ushort BlockId { get; set; }
    public TileLayer Layer { get; set; }
    public string PlayerName { get; set; } = "";
    public ulong PlayerSteamId { get; set; }
    
    public byte[] Serialize() 
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(BlockId);
        writer.Write((byte)Layer);
        writer.Write(PlayerName);
        writer.Write(PlayerSteamId);

        return stream.ToArray();
    }
    
    public static NetworkBlockChange Deserialize(byte[] data) 
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        return new NetworkBlockChange
        {
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            BlockId = reader.ReadUInt16(),
            Layer = (TileLayer)reader.ReadByte(),
            PlayerName = reader.ReadString(),
            PlayerSteamId = reader.ReadUInt64()
        };
    }
}