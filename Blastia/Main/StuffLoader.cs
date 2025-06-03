using System.Reflection;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Data;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.Items;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

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
						throw new FileNotFoundException($"[StuffLoader] Failed to load block texture for block: {blockType.Name}. Path: {texturePath}");
					}
					
					// register
					var texture = Util.LoadTexture(graphicsDevice, texturePath);					
					StuffRegistry.RegisterBlock(block, texture);
				}
			}
			catch (Exception e)
			{				
				throw new Exception($"[StuffLoader] Failed to load block: {blockType.Name}. Exception: {e.Message}");
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
			if (idAttribute == null) throw new Exception($"[StuffLoader] Entity must have an EntityAttribute! {humanType.Name}");
			
			var id = idAttribute.Id;
				
			string texturesFolder = $"{Paths.HumanTextures}/{humanType.Name}";
			
			var headTexture = Util.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "Head.png"));
			var bodyTexture = Util.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "Body.png"));
			var leftArmTexture = Util.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "LeftArm.png"));
			var rightArmTexture = Util.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "RightArm.png"));
			var legTexture = Util.LoadTexture(graphicsDevice, Path.Combine(texturesFolder, "Leg.png"));

			var textures = new HumanTextures(headTexture, bodyTexture, leftArmTexture,
				rightArmTexture, legTexture);
			StuffRegistry.RegisterHumanTextures(id, textures); // register textures first

			if (humanType == typeof(Player))
			{
				// player has additional bool argument
				var player = new Player(Vector2.Zero, null);
				StuffRegistry.RegisterHuman(id, player);
			}
			else if (Activator.CreateInstance(humanType, Vector2.Zero, 1f) is HumanLikeEntity human)
			{
				StuffRegistry.RegisterHuman(id, human);
			}
		}
	}

	public static void LoadItemsFromJson(GraphicsDevice graphicsDevice, string pathToJson)
	{
		if (!File.Exists(pathToJson))
		{
			Console.WriteLine($"[StuffLoader] Items JSON file not found. Path: {pathToJson}");
			return;
		}
		
		string jsonData = File.ReadAllText(pathToJson);
		List<DataDefinitions.ItemDefinition>? itemDefinitions;
		try
		{
			itemDefinitions = JsonConvert.DeserializeObject<List<DataDefinitions.ItemDefinition>>(jsonData);
		}
		catch (Exception e)
		{
			Console.WriteLine($"[StuffLoader] Failed to deserialize items JSON. Exception: {e.Message}");
			return;
		}

		if (itemDefinitions == null)
		{
			Console.WriteLine("[StuffLoader] Items JSON could not be parsed or was empty");
			return;
		}
		
		Console.WriteLine($"[StuffLoader] Loading {itemDefinitions.Count} item definitions");
		foreach (var def in itemDefinitions)
		{
			Texture2D icon;
			try
			{
				string fullIconPath = Path.Combine(Paths.ContentRoot, def.IconPath);
				if (!File.Exists(fullIconPath))
				{
					Console.WriteLine($"[StuffLoader] Icon not found at path: {fullIconPath} for item ID: {def.Id}");
					icon = BlastiaGame.WhitePixel;
				}
				else
				{
					icon = Util.LoadTexture(graphicsDevice, fullIconPath);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"[StuffLoader] Failed to load item icon: {e.Message}");
				icon = BlastiaGame.WhitePixel;
			}

			ItemType itemType = ItemType.Generic;
			if (!string.IsNullOrEmpty(def.Type))
			{
				if (Enum.TryParse<ItemType>(def.Type, true, out ItemType parsedType))
				{
					itemType = parsedType;
				}
				else
				{
					Console.WriteLine($"[StuffLoader] Unknown item type: {def.Type} for item ID: {def.Id}");
				}
			}

			Item item = new Item(def.Id, def.Name, def.Tooltip, icon, def.MaxStack, itemType);
			StuffRegistry.RegisterItem(item);
		}
		
		Console.WriteLine($"[StuffLoader] Successfully registered {itemDefinitions.Count} item definitions");
	}
}