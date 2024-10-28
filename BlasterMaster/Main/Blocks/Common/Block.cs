namespace BlasterMaster.Main.Blocks.Common;

[Serializable]
public abstract class Block 
{
	public static readonly int Size = 8;
	
	public abstract ushort ID { get; }
}