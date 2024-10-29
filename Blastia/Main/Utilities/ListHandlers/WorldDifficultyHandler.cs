namespace Blastia.Main.Utilities.ListHandlers;

public enum WorldDifficulty 
{
	Easy,
	Medium,
	Hard
}

public class WorldDifficultyHandler: EnumListHandler<WorldDifficulty> 
{
	public WorldDifficultyHandler() : base() 
	{
		
	}
	
	public string GetDifficultyName()
	{
		return CurrentItem switch
		{
			WorldDifficulty.Easy => "I am too young to die",
			WorldDifficulty.Medium => "Hurt me plenty",
			WorldDifficulty.Hard => "Nightmare",
			_ => throw new ArgumentOutOfRangeException()
		};
	}
		
	public override string GetString() => GetDifficultyName();
}