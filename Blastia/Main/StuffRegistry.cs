using System.Data;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main;

public static class StuffRegistry 
{
	// store Blocks by ID
	private static readonly Dictionary<ushort, Block> Blocks = new();
	// store Textures by ID
	private static readonly Dictionary<ushort, Texture2D> BlockTextures = new();

	private static readonly Dictionary<ushort, HumanLikeEntity> Humans = new();
	private static readonly Dictionary<ushort, HumanTextures> HumanTextures = new();
	
	public static void RegisterBlock(Block block, Texture2D texture)  
	{
		// if ID is already present in Blocks
		if (Blocks.ContainsKey(block.ID)) 
		{
			throw new DuplicateNameException($"Tried registering duplicate block with ID: {block.ID}, name: {block.GetType().Name}. " +
			                                 $"ID already occupied by {Blocks[block.ID].GetType().Name}");
		}
		
		// doesnt exist -> register
		Blocks[block.ID] = block;
		BlockTextures[block.ID] = texture;
	}
	
	public static Block? GetBlock(ushort id) 
	{
		// if found value -> return block
		return Blocks.TryGetValue(id, out var block) ? block : null;
	}
	
	public static Texture2D? GetTexture(ushort id) 
	{
		// if found texture -> return
		return BlockTextures.TryGetValue(id, out var texture) ? texture : null;
	}

	public static void RegisterHuman(HumanLikeEntity human, HumanTextures textures)
	{
		if (Humans.ContainsKey(human.ID))
		{
			throw new DuplicateNameException($"Tried registering duplicate human with ID: {human.ID}, name: {human.GetType().Name}. " +
			                                 $"ID already occupied by {Humans[human.ID].GetType().Name}");
		}

		Humans[human.ID] = human;
		HumanTextures[human.ID] = textures;
	}
	
	public static HumanLikeEntity? GetHuman(ushort id) 
	{
		return Humans.TryGetValue(id, out var human) ? human : null;
	}
	
	public static HumanTextures? GetHumanTextures(ushort id) 
	{
		return HumanTextures.TryGetValue(id, out var textures) ? textures : null;
	}
}