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
	public WorldSizeHandler() : base() 
	{
		
	}
	
	public int GetWidth()
	{
		return CurrentItem switch
		{
			WorldSize.Small => 1200,
			WorldSize.Medium => 3200,
			WorldSize.Large => 5600,
			WorldSize.XL => 8400,
			_ => throw new ArgumentOutOfRangeException()
		};
	}
	
	public int GetHeight()
	{
		return CurrentItem switch
		{
			WorldSize.Small => 400,
			WorldSize.Medium => 1066,
			WorldSize.Large => 1866,
			WorldSize.XL => 2800,
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}