namespace BlasterMaster.Main.Tiles;

[Serializable]
public class Block 
{
	public ushort ID { get; set; }
}

public static class BlockID 
{
	public static readonly ushort Air = 0;
	public static readonly ushort Dirt = 1;
	public static readonly ushort Stone = 2;
}