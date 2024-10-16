using System;
using System.Collections.Generic;
using System.Linq;

namespace BlasterMaster.Main.Utilities.ListHandlers;


public class EnumListHandler<EnumType> : ListHandler<EnumType> where EnumType : Enum 
{
	public EnumListHandler() : base(GetEnumValues()) 
	{
		
	}
	
	private static List<EnumType> GetEnumValues() 
	{
		// convert enum values to list
		return Enum.GetValues(typeof(EnumType)).Cast<EnumType>().ToList();
	}
	public override string GetString() => CurrentItem.ToString();
}