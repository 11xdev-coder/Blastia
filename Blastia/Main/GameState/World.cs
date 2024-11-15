using Blastia.Main.Utilities.ListHandlers;

namespace Blastia.Main.GameState;

public static class World 
{	
	public const float SmallBlocksAmount = WorldSizeHandler.SmallWorldWidth * WorldSizeHandler.SmallWorldHeight;
	public const float MediumBlocksAmount = WorldSizeHandler.MediumWorldWidth * WorldSizeHandler.MediumWorldHeight;
	public const float LargeBlocksAmount = WorldSizeHandler.LargeWorldWidth * WorldSizeHandler.LargeWorldHeight;
	public const float XlBlocksAmount = WorldSizeHandler.XlWorldWidth * WorldSizeHandler.XlWorldHeight;

	public static float GetBlocksAmount(WorldSize worldSize)
	{
		return worldSize switch
		{
			WorldSize.Small => SmallBlocksAmount,
			WorldSize.Medium => MediumBlocksAmount,
			WorldSize.Large => LargeBlocksAmount,
			WorldSize.XL => XlBlocksAmount,
			_ => throw new ArgumentOutOfRangeException()
		};
	}
	
	// volume of a block: 1 m3
	// mass of 1 m3 of stone: 1602 kg
	// mass of 1 m3 of dirt: 1300 kg
	// mass of all blocks  (36% stone 24% dirt 40% air): 
	public static double GetMass(WorldSize worldSize)
	{
		var amount = GetBlocksAmount(worldSize);

		var stoneMass = amount * 0.36 * 1602;
		var dirtMass = amount * 0.24 * 1300;
		var mass = stoneMass + dirtMass;

		return mass;
	}
}