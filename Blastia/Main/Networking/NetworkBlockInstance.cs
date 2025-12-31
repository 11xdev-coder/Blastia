using Blastia.Main.Blocks;
using Blastia.Main.Blocks.Common;

namespace Blastia.Main.Networking;

/// <summary>
/// Lightweight <c>BlockInstance</c> for networked blocks
/// </summary>
[Serializable]
public class NetworkBlockInstance
{
    public ushort Id;
    public float Damage { get; set; }
    public int FlowLevel { get; set; }
    
    public void FromBlockInstance(BlockInstance blockInstance)
    {
        Id = blockInstance.Id;
        Damage = blockInstance.Damage;
        
        if (blockInstance.Block is LiquidBlock liquid)
            FlowLevel = liquid.FlowLevel;
    }

    public BlockInstance? ToBlockInstance()
    {
        var block = StuffRegistry.GetBlock(Id);
        if (block == null) return null;

        var inst = new BlockInstance(block, Damage);
        if (inst.Block is LiquidBlock liquid)
        {
            liquid.FlowLevel = FlowLevel;
        }

        return inst;
    }

    public byte[] Serialize(MemoryStream stream, BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(Damage);
        writer.Write(FlowLevel);
        
        return stream.ToArray();
    }
    
    public NetworkBlockInstance Deserialize(BinaryReader reader)
    {
        var inst = new NetworkBlockInstance
        {
            Id = reader.ReadUInt16(),
            Damage = reader.ReadSingle(),
            FlowLevel = reader.ReadInt32()
        };

        return inst;
    }
}