using BlasterMaster.Main.Utilities.ListHandlers;

public enum WorldDifficulty 
{
	IAmTooYoungToDie,
	HurtMePlenty,
	Nightmare
}

public class WorldDifficultyHandler: EnumListHandler<WorldDifficulty> 
{
	public WorldDifficultyHandler() : base() 
	{
		
	}
	
	public string GetDifficultyDescription()
    {
        return CurrentItem switch
        {
            WorldDifficulty.IAmTooYoungToDie => "For babies",
            WorldDifficulty.HurtMePlenty => "The intended challenge",
            WorldDifficulty.Nightmare => "gdggdgdgd",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}