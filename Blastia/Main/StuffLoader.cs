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
	
	#region Blocks
	public static void LoadBlocks(GraphicsDevice graphicsDevice) 
	{
		LoadScriptedBlocks(graphicsDevice);
		LoadSimpleBlocksFromJson(graphicsDevice, Paths.BlocksData);
	}

	private static void LoadScriptedBlocks(GraphicsDevice graphicsDevice)
	{
		// get all Block classes in namespace (exclude SimpleBlock)
		var blockTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t => t.Namespace == BlocksNamespace && !t.IsAbstract && t.IsClass && typeof(Block).IsAssignableFrom(t) && t != typeof(SimpleBlock));
		
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
						throw new FileNotFoundException($"[StuffLoader] [ScriptedBlocks] Failed to load block texture for block: {blockType.Name}. Path: {texturePath}");
					}
					
					// register
					var texture = Util.LoadTexture(graphicsDevice, texturePath);					
					StuffRegistry.RegisterBlock(block, texture);
					Console.WriteLine($"[StuffLoader] [ScriptedBlocks] Loaded scripted block: {block.Name} (ID: {block.Id})");
				}
			}
			catch (Exception e)
			{				
				throw new Exception($"[StuffLoader] [ScriptedBlocks] Failed to load block: {blockType.Name}. Exception: {e.Message}");
			}
		}
	}

	private static void LoadSimpleBlocksFromJson(GraphicsDevice graphicsDevice, string pathToJson)
	{
		if (!File.Exists(pathToJson))
		{
			Console.WriteLine($"[StuffLoader] [SimpleBlocks] JSON not found at path: {pathToJson}");
			return;
		}
		
		var jsonData = File.ReadAllText(pathToJson);
		List<DataDefinitions.BlockDefinition>? blockDefinitions;
		try
		{
			blockDefinitions = JsonConvert.DeserializeObject<List<DataDefinitions.BlockDefinition>>(jsonData);
		}
		catch (Exception e)
		{
			Console.WriteLine($"[StuffLoader] [SimpleBlocks] Failed to deserialize blocks JSON: {e.Message}");
			return;
		}

		if (blockDefinitions == null)
		{
			Console.WriteLine("[StuffLoader] [SimpleBlocks] Blocks JSON could not be parsed or was empty");
			return;
		}
		
		Console.WriteLine($"[StuffLoader] [SimpleBlocks] Loading {blockDefinitions.Count} block definitions");
		foreach (var def in blockDefinitions)
		{
			try
			{
				// load texture
				Texture2D texture;
				var fullTexturePath = Path.Combine(Paths.ContentRoot, def.TexturePath);
				if (!File.Exists(fullTexturePath))
				{
					Console.WriteLine($"[StuffLoader] [SimpleBlocks] Texture not found at {fullTexturePath}");
					texture = BlastiaGame.WhitePixel;
				}
				else
				{
					texture = Util.LoadTexture(graphicsDevice, fullTexturePath);
				}

				var block = new SimpleBlock(def.Id, def.Name, def.DragCoefficient, def.Hardness, def.IsCollidable,
					def.IsTransparent, def.ItemIdDrop, def.ItemDropAmount, def.LightLevel);
				StuffRegistry.RegisterBlock(block, texture);
				Console.WriteLine($"[StuffLoader] [SimpleBlocks] Loaded simple block: {block.Name} (ID: {block.Id})");
			}
			catch (Exception e)
			{
				Console.WriteLine($"[StuffLoader] [SimpleBlocks] Failed to create a block {def.Id}: {e.Message}");
			}
		}
		Console.WriteLine($"[StuffLoader] [SimpleBlocks] Loaded {blockDefinitions.Count} block definitions");
	}
	#endregion

	#region Humans
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
	#endregion

	#region Items
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
			try
			{
				var item = CreateItemForDefinition(def, graphicsDevice);
				StuffRegistry.RegisterItem(item);
			}
			catch (Exception e)
			{
				Console.WriteLine($"[StuffLoader] Failed to create item {def.Id}. Exception: {e.Message}");
			}
		}
		
		Console.WriteLine($"[StuffLoader] Successfully registered {itemDefinitions.Count} item definitions");
	}

	private static Item CreateItemForDefinition(DataDefinitions.ItemDefinition definition, GraphicsDevice graphicsDevice)
	{
		// load icon
		Texture2D icon;
		try
		{
			var fullIconPath = Path.Combine(Paths.ContentRoot, definition.IconPath);
			if (!File.Exists(fullIconPath))
			{
				Console.WriteLine($"[StuffLoader] Icon not found at path: {fullIconPath} for item ID: {definition.Id}");
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
		
		// parse item type
		var itemType = ItemType.Generic;
		if (!string.IsNullOrEmpty(definition.Type))
		{
			if (!Enum.TryParse(definition.Type, true, out itemType))
			{
				Console.WriteLine($"[StuffLoader] Unknown item type: {definition.Type} for item ID: {definition.Id}");
				itemType = ItemType.Generic;
			}
		}
		
		// create specific item type
		return itemType switch
		{
			ItemType.Placeable => CreatePlaceableItem(definition, icon),
			_ => new GenericItem(definition.Id, definition.Name, definition.Tooltip, icon, definition.MaxStack)
		};
	}

	private static PlaceableItem CreatePlaceableItem(DataDefinitions.ItemDefinition def, Texture2D icon)
	{
		ushort blockId = 0;
		var placeSound = "";

		if (def.Properties != null)
		{
			blockId = def.Properties.Value<ushort>("BlockId");
			placeSound = def.Properties.Value<string>("PlaceSound");
		}
		
		return new PlaceableItem(def.Id, def.Name, def.Tooltip, icon, def.MaxStack, blockId, placeSound);
	}
	#endregion
}