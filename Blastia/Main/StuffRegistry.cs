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
		if (!Blocks.TryAdd(block.ID, block)) 
		{
			throw new DuplicateNameException($"Duplicate block with ID: {block.ID}");
		}
		
		// doesnt exist -> register
		BlockTextures[block.ID] = texture;
	}
	
	public static Block? GetBlock(ushort id) 
	{
		// if found value -> return block
		return Blocks.GetValueOrDefault(id);
	}
	
	public static Texture2D? GetTexture(ushort id) 
	{
		// if found texture -> return
		return BlockTextures.GetValueOrDefault(id);
	}

	// HUMANS
	public static void RegisterHumanTextures(ushort id, HumanTextures textures)
	{
		if (!HumanTextures.TryAdd(id, textures))
		{
			throw new DuplicateNameException($"Duplicate textures for human with ID: {id}");
		}
	}
	
	public static void RegisterHuman(ushort id, HumanLikeEntity human)
	{
		if (!Humans.TryAdd(id, human))
		{
			throw new DuplicateNameException($"Duplicate human with ID: {id}");
		}
	}
	
	public static HumanLikeEntity? GetHuman(ushort id) 
	{
		return Humans.GetValueOrDefault(id);
	}
	
	public static HumanTextures? GetHumanTextures(ushort id)
	{
		return HumanTextures.GetValueOrDefault(id);
	}
}