using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Networking;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;

namespace Blastia.Main;

public enum SaveFolder { Player, World }
public enum Extension { Player, World }

public class PlayerNWorldManager : Singleton<PlayerNWorldManager>
{
	private string _playersSaveFolder = "";
	public string WorldsSaveFolder = "";
	
	public PlayerState? SelectedPlayer { get; private set; }
	public WorldState? SelectedWorld { get; private set; }

	public void Initialize()
	{
		_playersSaveFolder = Path.Combine(Paths.GetSaveGameDirectory(), "Players");
		WorldsSaveFolder = Path.Combine(Paths.GetSaveGameDirectory(), "Worlds");
		
		// create folders if dont exist
		if (!Directory.Exists(_playersSaveFolder)) Directory.CreateDirectory(_playersSaveFolder);
		if (!Directory.Exists(WorldsSaveFolder)) Directory.CreateDirectory(WorldsSaveFolder);
	}

	private void New(SaveFolder folderType, string name, Extension extensionType, object? data = null)
	{
		string folder = GetFolder(folderType);
		string extension = GetExtension(extensionType);
		
		if (!string.IsNullOrEmpty(folder))
		{
			string fileName = GetPath(folder, name, extension);

			if (Directory.Exists(folder) && !File.Exists(fileName))
			{
				// save data if provided
				if (data != null) 
				{
					Saving.Save(fileName, data);
				}
				else 
				{
					File.Create(fileName).Close();
				}				
			}
		}
		else throw new Exception("Provided folder path is null.");
	}

	private bool Exists(SaveFolder folderType, string name, Extension extensionType)
	{
		string folder = GetFolder(folderType);
		string extension = GetExtension(extensionType);
		
		if (!string.IsNullOrEmpty(folder))
		{
			// search at folder/name.extension
			string fileName = GetPath(folder, name, extension);
			return File.Exists(fileName);
		}
		
		throw new Exception("Provided folder path is null.");
	}

	private List<T> LoadAll<T>(SaveFolder folderType, Extension extensionType)
		where T : new()
	{
		string folder = GetFolder(folderType);
		string extension = GetExtension(extensionType);
		
		if (!string.IsNullOrEmpty(folder))
		{
			List<T> items = new List<T>();

			// go through each file
			foreach (string file in Directory.GetFiles(folder))
			{
				// if correct extension
				if (file.EndsWith(extension))
				{
					// load new instance
					var loadedState = Saving.Load<T>(file);
					items.Add(loadedState);				
				}
			}

			return items;
		}
		
		// return nothing if path is not initialized
		return new List<T>();
	}

	private string GetPath(string folder, string name, string extension)
	{
		// Players/Name.bmplr or Worlds/Name.bmwld
		return Path.Combine(folder, name + extension);
	}
	
	private string GetFolder(SaveFolder folderType) 
	{
		string folder = folderType switch 
		{
			SaveFolder.Player => _playersSaveFolder,
			SaveFolder.World => WorldsSaveFolder,
			_ => ""
		};
		
		return folder;
	}
	
	private string GetExtension(Extension extensionType) 
	{
		string extension = extensionType switch
		{
			Extension.Player => ".bmplr",
			Extension.World => ".bmwld",
			_ => ""
		};
		
		return extension;
	}
	
	// PLAYER
	public void NewPlayer(string playerName) 
	{
		PlayerState playerData = new() 
		{
			Name = playerName
		};
		New(SaveFolder.Player, playerName, Extension.Player, playerData);
	}
	public bool PlayerExists(string playerName) => Exists(SaveFolder.Player, playerName, Extension.Player);
	public List<PlayerState> LoadAllPlayers() => LoadAll<PlayerState>(SaveFolder.Player, Extension.Player);

	public void SelectPlayer(PlayerState playerState)
	{
		SelectedPlayer = playerState;
	}
	
	public void UnselectPlayer() => SelectedPlayer = null;
	
	// WORLD
	public void NewWorld(string worldName, WorldDifficulty difficulty = WorldDifficulty.Easy, 
			int worldWidth = 0, int worldHeight = 0) 
	{
		WorldState worldData = new WorldState 
		{ 
			Name = worldName, 
			Difficulty = difficulty,
			WorldWidth = worldWidth,
			WorldHeight = worldHeight
		};
		WorldGen.Generate(52, worldData);
		
		New(SaveFolder.World, worldName, Extension.World, worldData);
	}
	
	public bool WorldExists(string worldName) => Exists(SaveFolder.World, worldName, Extension.World);
	public List<WorldState> LoadAllWorlds() => LoadAll<WorldState>(SaveFolder.World, Extension.World);
		
	public void SelectWorld(WorldState worldState, bool host)
	{
		SelectedWorld = worldState;
		BlastiaGame.RequestWorldInitialization();

		if (host)
		{
			NetworkManager.Instance?.HostGame();
		}
	}

	public void UnselectWorld() => SelectedWorld = null;
}

[Serializable]
public class PlayerState
{
	public string Name { get; set; } = "";
	public override string ToString() => Name;
}

[Serializable]
public class WorldState
{
	public string Name { get; set; } = "";
	public override string ToString() => Name;
	public WorldDifficulty Difficulty { get; set; } = WorldDifficulty.Easy;
	
	// 1D to support serialization
	public Dictionary<Vector2, ushort> Tiles { get; set; } = new();
	public Dictionary<Vector2, BlockInstance> TileInstances { get; set; } = new();
	public int WorldWidth { get; set; }
	public int WorldHeight { get; set; }

	// sign position -> sign text
	public Dictionary<Vector2, string> SignTexts { get; set; } = [];

	public float SpawnX { get; set; }
	public float SpawnY { get; set; }

	public bool SetTileLogs { get; set; } = false;
	
	/// <summary>
	/// Sets tile's ID at x y coords. If new ID is 0 -> removes the tile completely
	/// </summary>
	/// <param name="alignedX"></param>
	/// <param name="alignedY"></param>
	/// <param name="value">New ID</param>
	/// <param name="player">Player that set the tile</param>
	public void SetTile(int alignedX, int alignedY, ushort value, Player? player = null)
	{
		Vector2 pos = new(alignedX, alignedY);
		if (SetTileLogs) Console.WriteLine($"World: {Name}, Set tile at: (X: {alignedX}, Y: {alignedY}), ID: {value}");
		
		if (value == 0)
		{
			// if new ID is air (0) -> remove tile to save space
			// first call its OnBreak
			if (TileInstances.TryGetValue(pos, out var blockInstance))
			{
				blockInstance.OnBreak(player?.World, pos, player);
				TileInstances.Remove(pos);
			}
			Tiles.Remove(pos);
		}
		else
		{
			Tiles[pos] = value;

			var block = StuffRegistry.GetBlock(value);
			if (block == null)
			{
				if (SetTileLogs) Console.WriteLine($"Block ID: {value} not found in registry");
			}
			else
			{
				var blockInstance = new BlockInstance(block, 0);
				blockInstance.OnPlace(player?.World, pos, player);
				TileInstances[pos] = blockInstance;
			}
		}
	}

	/// <summary>
	/// Sets block instance at coordinates
	/// </summary>
	/// <param name="alignedX"></param>
	/// <param name="alignedY"></param>
	/// <param name="block">New block instance, must not be empty</param>
	public void SetTileInstance(int alignedX, int alignedY, BlockInstance block)
	{
		Vector2 pos = new(alignedX, alignedY);
		Tiles[pos] = block.Id;
		TileInstances[pos] = block;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="alignedX">X aligned to Block.Size</param>
	/// <param name="alignedY">Y aligned to Block.Size</param>
	/// <returns></returns>
	public ushort GetTile(int alignedX, int alignedY)
	{
		Vector2 pos = new(alignedX, alignedY);
		if (Tiles.TryGetValue(pos, out ushort id))
		{
			return id;
		}
		return 0; // Air
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="alignedX">X aligned to Block.Size</param>
	/// <param name="alignedY">Y aligned to Block.Size</param>
	/// <returns></returns>
	public BlockInstance? GetBlockInstance(int alignedX, int alignedY)
	{
		var pos = new Vector2(alignedX, alignedY);
		if (TileInstances.TryGetValue(pos, out var blockInstance))
		{
			return blockInstance;
		}
		return null;
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="worldX">World (not aligned) X</param>
	/// <param name="worldY">World (not aligned) Y</param>
	/// <returns></returns>
	public ushort GetTileAtWorldCoord(int worldX, int worldY)
	{
		int tileWorldX = (int)Math.Floor((float)worldX / Block.Size) * Block.Size;
		int tileWorldY = (int)Math.Floor((float)worldY / Block.Size) * Block.Size;
		return GetTile(tileWorldX, tileWorldY);
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="worldX">World (not aligned) X</param>
	/// <param name="worldY">World (not aligned) Y</param>
	/// <returns></returns>
	public BlockInstance? GetBlockInstanceAtWorldCoord(int worldX, int worldY)
	{
		int tileWorldX = (int)Math.Floor((float)worldX / Block.Size) * Block.Size;
		int tileWorldY = (int)Math.Floor((float)worldY / Block.Size) * Block.Size;
		return GetBlockInstance(tileWorldX, tileWorldY);
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="worldX">World (not aligned) X</param>
	/// <param name="worldY">World (not aligned) Y</param>
	/// <returns><c>True</c> if tile at coordinates is not air</returns>
	public bool HasTile(int worldX, int worldY)
	{
		int tileWorldX = (int)Math.Floor((float)worldX / Block.Size) * Block.Size;
		int tileWorldY = (int)Math.Floor((float)worldY / Block.Size) * Block.Size;

		ushort tileId = GetTile(tileWorldX, tileWorldY);
		return tileId >= 1;
	}

	
	/// <summary>
	/// Helper method to find first non-air tile below an entity
	/// </summary>
	/// <param name="entityLeftX">Entity left edge world X</param>
	/// <param name="entityBottomY">Entity bottom edge world Y</param>
	/// <param name="entityWidthPixels">Entity width (in pixels)</param>
	/// <param name="checkDistance">Distance to check for tile below</param>
	/// <returns>Tuple containing tile ID and coordinates, returns <c>(BlockId.Air, 0, 0)</c> if none found</returns>
	private (ushort tileId, int x, int y) GetFirstTileBelowWithCoords(float entityLeftX, float entityBottomY,
		float entityWidthPixels, float checkDistance)
	{
		var checkWorldY = (int) (entityBottomY + checkDistance);

		var checkWorldXLeft = (int) (entityLeftX + 1);
		var checkWorldXCenter = (int) (entityLeftX + entityWidthPixels * 0.5f);
		var checkWorldXRight = (int) (entityLeftX + entityWidthPixels - 1);
		
		// get tile IDs
		var tileIdCenter = GetTileAtWorldCoord(checkWorldXCenter, checkWorldY);
		if (tileIdCenter != BlockId.Air)
			return (tileIdCenter, checkWorldXCenter, checkWorldY);
		
		var tileIdLeft = GetTileAtWorldCoord(checkWorldXLeft, checkWorldY);
		if (tileIdLeft != BlockId.Air)
			return (tileIdLeft, checkWorldXLeft, checkWorldY);
		
		var tileIdRight = GetTileAtWorldCoord(checkWorldXRight, checkWorldY);
		if (tileIdRight != BlockId.Air)
			return (tileIdRight, checkWorldXRight, checkWorldY);

		return (BlockId.Air, 0, 0);
	} 
	
	/// <summary>
	/// Returns tile ID of the tile below the entity's feet
	/// </summary>
	/// <param name="entityLeftX">Entity left edge world X</param>
	/// <param name="entityBottomY">Entity bottom edge world Y</param>
	/// <param name="entityWidthPixels">Entity width (in pixels)</param>
	/// <param name="checkDistance">Distance to check for tile below</param>
	/// <returns>If tile was found returns its ID, otherwise returns <c>BlockID.Air</c></returns>
	public ushort GetTileIdBelow(float entityLeftX, float entityBottomY, float entityWidthPixels, float checkDistance)
	{
		var (tileId, _, _) = GetFirstTileBelowWithCoords(entityLeftX, entityBottomY, entityWidthPixels, checkDistance);
		return tileId;
	}
	
	
	/// <summary>
	/// Returns block instance below the entity's feet
	/// </summary>
	/// <param name="entityLeftX">Entity left edge world X</param>
	/// <param name="entityBottomY">Entity bottom edge world Y</param>
	/// <param name="entityWidthPixels">Entity width (in pixels)</param>
	/// <param name="checkDistance">Distance to check for tile below</param>
	/// <returns>If tile was found returns its <c>BlockInstance</c>, otherwise returns null</returns>
	public BlockInstance? GetBlockInstanceBelow(float entityLeftX, float entityBottomY, float entityWidthPixels, float checkDistance)
	{
		var (tileId, x, y) = GetFirstTileBelowWithCoords(entityLeftX, entityBottomY, entityWidthPixels, checkDistance);
		if (tileId == BlockId.Air) 
			return null;

		return GetBlockInstanceAtWorldCoord(x, y);
	}

	/// <summary>
	/// Gets the drag coefficient of the tile directly beneath the entity's feet
	/// by checking multiple points along its bottom edge
	/// </summary>
	/// <param name="entityLeftX">Entity left edge world X</param>
	/// <param name="entityBottomY">Entity bottom edge world Y</param>
	/// <param name="entityWidthPixels">Entity width (in pixels)</param>
	/// <returns>If tile was found returns its drag, otherwise returns <c>Block.AirDragCoefficient</c></returns>
	public float GetDragCoefficientTileBelow(float entityLeftX, float entityBottomY, float entityWidthPixels)
	{
	    var tileId = GetTileIdBelow(entityLeftX, entityBottomY, entityWidthPixels, 1f);
	    var block = StuffRegistry.GetBlock(tileId);
	    if (block != null)
	    {
	        return block.DragCoefficient;
	    }

	    return Block.AirDragCoefficient;
	}

	public void SetSpawnPoint(float x, float y)
	{
		// from tiles to world coords
		SpawnX = x;
		SpawnY = y;
	}

	public Vector2 GetSpawnPoint() => new(SpawnX, SpawnY);

	public void LogTiles()
	{
		foreach (var kvp in Tiles)
		{
			Console.WriteLine($"Tile at (X: {kvp.Key.X}; Y: {kvp.Key.Y}), ID: {kvp.Value}");
		}
	}
}