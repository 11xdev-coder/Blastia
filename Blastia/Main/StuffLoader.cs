using System.Reflection;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main;

public static class StuffLoader 
{
	private const string BlocksNamespace = "Blastia.Main.Blocks";
	private const string HumanEntitiesNamespace = "Blastia.Main.Entities.HumanLikeEntities";
	
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
					StuffRegistry.RegisterBlock(block, texture);
				}
			}
			catch (Exception e)
			{				
				throw new Exception($"Failed to load block: {blockType.Name}. Exception: {e.Message}");
			}
		}
	}

	public static void LoadHumans(GraphicsDevice graphicsDevice)
	{
		var humanTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t => t.Namespace == HumanEntitiesNamespace && !t.IsAbstract && t.IsClass && 
			            typeof(HumanLikeEntity).IsAssignableFrom(t));

		foreach (var humanType in humanTypes)
		{
			if (Activator.CreateInstance(humanType, Vector2.Zero, 1f) is HumanLikeEntity human)
			{
				string texturesFolder = $"{Paths.HumanTextures}/{humanType.Name}";
				
				string head = Path.Combine(texturesFolder, "Head.png");
				string body = Path.Combine(texturesFolder, "Body.png");
				string leftArm = Path.Combine(texturesFolder, "LeftArm.png");
				string rightArm = Path.Combine(texturesFolder, "RightArm.png");
				string leg = Path.Combine(texturesFolder, "Leg.png");
				
				var headTexture = LoadingUtilities.LoadTexture(graphicsDevice, head);
				var bodyTexture = LoadingUtilities.LoadTexture(graphicsDevice, body);
				var leftArmTexture = LoadingUtilities.LoadTexture(graphicsDevice, leftArm);
				var rightArmTexture = LoadingUtilities.LoadTexture(graphicsDevice, rightArm);
				var legTexture = LoadingUtilities.LoadTexture(graphicsDevice, leg);
				
				StuffRegistry.RegisterHuman(human, new HumanTextures(headTexture, bodyTexture, leftArmTexture,
					rightArmTexture, legTexture));
			}
		}
	}
}