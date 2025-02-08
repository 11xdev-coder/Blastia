namespace Blastia.Main.Utilities.ListHandlers;


public class EnumListHandler<TEnumType> : ListHandler<TEnumType> where TEnumType : Enum 
{
	public EnumListHandler() : base(GetEnumValues()) 
	{
		
	}
	
	private static List<TEnumType> GetEnumValues() 
	{
		// convert enum values to list
		return Enum.GetValues(typeof(TEnumType)).Cast<TEnumType>().ToList();
	}
	public override string GetString() => CurrentItem.ToString();
}