using System.Data;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Blocks.Common;

public static class BlockRegistry 
{
	// store Blocks by ID
	private static readonly Dictionary<ushort, Block> Blocks = new();
	// store Textures by ID
	private static readonly Dictionary<ushort, Texture2D> BlockTextures = new();
	
	public static void RegisterBlock(Block block, Texture2D texture)  
	{
		// if ID is already present in Blocks
		if (Blocks.ContainsKey(block.ID)) 
		{
			throw new Exception($"Tried registering duplicate block with ID: {block.ID}, name: {block.GetType().Name}. \n" +
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
}