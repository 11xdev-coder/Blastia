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

public class PlayerManager : Singleton<PlayerManager>
{
	private string _playersSaveFolder = "";
	private string _worldsSaveFolder = "";
	
	public PlayerState? SelectedPlayer { get; private set; }
	public WorldState? SelectedWorld { get; private set; }

	public void Initialize()
	{
		_playersSaveFolder = Path.Combine(Paths.GetSaveGameDirectory(), "Players");
		_worldsSaveFolder = Path.Combine(Paths.GetSaveGameDirectory(), "Worlds");
		
		// create folders if dont exist
		if (!Directory.Exists(_playersSaveFolder)) Directory.CreateDirectory(_playersSaveFolder);
		if (!Directory.Exists(_worldsSaveFolder)) Directory.CreateDirectory(_worldsSaveFolder);
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
			SaveFolder.World => _worldsSaveFolder,
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
	
	private void GenerateWorldTiles(WorldState worldState) 
	{
		
	}
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
	public Dictionary<Vector2, Block> TileInstances { get; set; } = new();
	public int WorldWidth { get; set; }
	public int WorldHeight { get; set; }

	public float SpawnX { get; set; }
	public float SpawnY { get; set; }

	public bool SetTileLogs { get; set; } = false;
	
	/// <summary>
	/// Sets tile's ID at x y coords. If new ID is 0 -> removes the tile completely
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="value">New ID</param>
	/// <param name="player">Player that set the tile</param>
	public void SetTile(int x, int y, ushort value, Player? player = null)
	{
		Vector2 pos = new(x, y);
		if (SetTileLogs) Console.WriteLine($"World: {Name}, Set tile at: (X: {x}, Y: {y}), ID: {value}");
		
		if (value == 0)
		{
			// if new ID is air (0) -> remove tile to save space
			// first call its OnBreak
			if (TileInstances.TryGetValue(pos, out var block))
			{
				block.OnBreak(player?.World, pos, player);
				TileInstances.Remove(pos);
			}
			Tiles.Remove(pos);
		}
		else
		{
			Tiles[pos] = value;

			var blockInstance = StuffRegistry.GetBlock(value);
			if (blockInstance == null)
			{
				Console.WriteLine($"[SetTile] Tried setting block at: (X: {x}, Y: {y}), ID: {value}, but instance is null");
			}
			else
			{
				TileInstances[pos] = blockInstance;
			}
		}
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
	/// <param name="worldX">World (not aligned) X</param>
	/// <param name="worldY">World (not aligned) Y</param>
	/// <returns></returns>
	private ushort GetTileAtWorldCoord(int worldX, int worldY)
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
	/// <returns><c>True</c> if tile at coordinates is not air</returns>
	public bool HasTile(int worldX, int worldY)
	{
		int tileWorldX = (int)Math.Floor((float)worldX / Block.Size) * Block.Size;
		int tileWorldY = (int)Math.Floor((float)worldY / Block.Size) * Block.Size;

		ushort tileId = GetTile(tileWorldX, tileWorldY);
		return tileId >= 1;
	}

	/// <summary>
	/// Return tile ID of the tile below the entity's feet
	/// </summary>
	/// <param name="entityLeftX">Entity left edge world X</param>
	/// <param name="entityBottomY">Entity bottom edge world Y</param>
	/// <param name="entityWidthPixels">Entity width (in pixels)</param>
	/// <param name="checkDistance">Distance to check for tile below</param>
	/// <returns>If tile was found returns its ID, otherwise returns <c>BlockID.Air</c></returns>
	public ushort GetTileIdBelow(float entityLeftX, float entityBottomY, float entityWidthPixels, float checkDistance)
	{
		int checkWorldY = (int)(entityBottomY + checkDistance);

		int checkWorldXLeft = (int)(entityLeftX + 1);
		int checkWorldXCenter = (int)(entityLeftX + entityWidthPixels * 0.5f);
		int checkWorldXRight = (int)(entityLeftX + entityWidthPixels - 1);

		// Get the Tile IDs at the check locations using the corrected HasTile logic
		ushort tileIdLeft = GetTileAtWorldCoord(checkWorldXLeft, checkWorldY);
		ushort tileIdCenter = GetTileAtWorldCoord(checkWorldXCenter, checkWorldY);
		ushort tileIdRight = GetTileAtWorldCoord(checkWorldXRight, checkWorldY);

		ushort finalTileId = BlockId.Air; 
		if (tileIdCenter != BlockId.Air)
		{
			finalTileId = tileIdCenter;
		}
		else if (tileIdLeft != BlockId.Air)
		{
			finalTileId = tileIdLeft;
		}
		else if (tileIdRight != BlockId.Air)
		{
			finalTileId = tileIdRight;
		}
		
		return finalTileId;
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