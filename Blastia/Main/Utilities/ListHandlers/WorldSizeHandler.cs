namespace Blastia.Main.Utilities.ListHandlers;

public enum WorldSize 
{
	Small,
	Medium,
	Large,
	XL
}

public class WorldSizeHandler: EnumListHandler<WorldSize>
{
	// world sizes
	public const int SmallWorldWidth = 1200;
	public const int SmallWorldHeight = 400;
	
	public const int MediumWorldWidth = 3200;
	public const int MediumWorldHeight = 1066;
	
	public const int LargeWorldWidth = 5600;
	public const int LargeWorldHeight = 1866;
	
	public const int XlWorldWidth = 8400;
	public const int XlWorldHeight = 2800;
	
	public WorldSizeHandler() : base() 
	{
		
	}
	
	public int GetWidth()
	{
		return CurrentItem switch
		{
			WorldSize.Small => SmallWorldWidth,
			WorldSize.Medium => MediumWorldWidth,
			WorldSize.Large => LargeWorldWidth,
			WorldSize.XL => XlWorldWidth,
			_ => throw new ArgumentOutOfRangeException()
		};
	}
	
	public int GetHeight()
	{
		return CurrentItem switch
		{
			WorldSize.Small => SmallWorldHeight,
			WorldSize.Medium => MediumWorldHeight,
			WorldSize.Large => LargeWorldHeight,
			WorldSize.XL => XlWorldHeight,
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}