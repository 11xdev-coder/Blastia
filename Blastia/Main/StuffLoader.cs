using System.Reflection;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
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
			var idAttribute = humanType.GetCustomAttribute<EntityAttribute>();
			if (idAttribute == null) throw new Exception($"Entity must have an EntityAttribute! {humanType.Name}");
			
			var id = idAttribute.Id;
				
			string texturesFolder = $"{Paths.HumanTextures}/{humanType.Name}";
			
			var headTexture = LoadingUtilities.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "Head.png"));
			var bodyTexture = LoadingUtilities.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "Body.png"));
			var leftArmTexture = LoadingUtilities.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "LeftArm.png"));
			var rightArmTexture = LoadingUtilities.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "RightArm.png"));
			var legTexture = LoadingUtilities.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "Leg.png"));

			var textures = new HumanTextures(headTexture, bodyTexture, leftArmTexture,
				rightArmTexture, legTexture);
			StuffRegistry.RegisterHumanTextures(id, textures); // register textures first

			if (Activator.CreateInstance(humanType, Vector2.Zero, 1f) is HumanLikeEntity human)
			{
				StuffRegistry.RegisterHuman(id, human);
			}
		}
	}
}