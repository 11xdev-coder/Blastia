using System.Reflection;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks.Common;

public static class BlockLoader 
{
	private const string BlocksNamespace = "Blastia.Main.Blocks";
	
	public static void LoadBlocks(GraphicsDevice graphicsDevice) 
	{
		// get all Block classes in namespace 
		var blockTypes = Assembly.GetExecutingAssembly().GetTypes()
		.Where(t => t.Namespace == BlocksNamespace && !t.IsAbstract && t.IsClass && typeof(Block).IsAssignableFrom(t));
		
		foreach (var blockType in blockTypes) 
		{
			try
			{
				// create instance of blockType as Block
				if (Activator.CreateInstance(blockType) is Block block) 
				{
					// load texture
					string texturePath = $"{Paths.BlockTextures}/{blockType.Name}.png";
					
					if (!File.Exists(texturePath)) 
					{
						throw new FileNotFoundException($"Failed to load block texture for block: {blockType.Name}. Path: {texturePath}");
					}
					
					// register
					var texture = LoadingUtilities.LoadTexture(graphicsDevice, texturePath);					
					BlockRegistry.RegisterBlock(block, texture);
				}
			}
			catch (Exception e)
			{				
				throw new Exception($"Failed to load block: {blockType.Name}. Exception: {e.Message}");
			}
		}
	}
}