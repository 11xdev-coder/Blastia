using System.Data;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.Items;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main;

public static class StuffRegistry 
{
	// BLOCKS
	// store Blocks by ID
	private static readonly Dictionary<ushort, Block> Blocks = new();
	// store Textures by ID
	private static readonly Dictionary<ushort, Texture2D> BlockTextures = new();

	// HUMANS
	// store humanoids by ID
	private static readonly Dictionary<ushort, HumanLikeEntity> Humans = new();
	// store human textures by ID
	private static readonly Dictionary<ushort, HumanTextures> HumanTextures = new();
	
	// ITEMS
	private static readonly Dictionary<ushort, Item> Items = new();
	
	#region Blocks
	public static void RegisterBlock(Block block, Texture2D texture)  
	{
		// if ID is already present in Blocks
		if (!Blocks.TryAdd(block.Id, block)) 
		{
			throw new DuplicateNameException($"Duplicate block with ID: {block.Id}");
		}
		
		// doesnt exist -> register
		BlockTextures[block.Id] = texture;
	}
	
	public static Block? GetBlock(ushort id) 
	{
		// if found value -> return block
		var block = Blocks.GetValueOrDefault(id);
		// clone to avoid reference issues
		return block?.Clone();
	}
	
	public static Texture2D? GetBlockTexture(ushort id) 
	{
		// if found texture -> return
		return BlockTextures.GetValueOrDefault(id);
	}

	#endregion
	
	#region Humanoids
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
	
	#endregion
	
	#region Items
	public static void RegisterItem(Item item)
	{
		// register item texture if it doesnt exist
		if (!Items.TryAdd(item.Id, item))
		{
			throw new DuplicateNameException($"[StuffRegistry] Duplicate item with ID: {item.Id}");
		}
	}

	public static Item? GetItem(ushort id)
	{
		return Items.GetValueOrDefault(id);
	}
	
	#endregion
}