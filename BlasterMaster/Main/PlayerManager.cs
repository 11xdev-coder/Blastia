using System.Text.Json;
using BlasterMaster.Main.Blocks;
using BlasterMaster.Main.Blocks.Common;
using BlasterMaster.Main.Utilities;
using BlasterMaster.Main.Utilities.ListHandlers;

namespace BlasterMaster.Main;

public class PlayerManager : Singleton<PlayerManager>
{
	private string? _playersSaveFolder;
	private string? _worldsSaveFolder;
	
	public PlayerState? SelectedPlayer { get; private set; }
	public WorldState? SelectedWorld { get; private set; }

	public void Initialize(string playersSaveFolder, string worldsSaveFolder)
	{
		_playersSaveFolder = playersSaveFolder;
		_worldsSaveFolder = worldsSaveFolder;
	}

	private void New(string? folder, string name, string extension, object? data = null)
	{
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
		else throw new Exception("Save path not initialized.");
	}

	private bool Exists(string? folder, string name, string extension)
	{
		if (!string.IsNullOrEmpty(folder))
		{
			// search at folder/name.extension
			string fileName = GetPath(folder, name, extension);
			return File.Exists(fileName);
		}
		
		throw new Exception("Save path not initialized.");
	}

	private List<T> LoadAll<T>(string? folder, string extension)
		where T : new()
	{
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
	
	// PLAYER
	public void NewPlayer(string playerName) 
	{
		PlayerState stateData = new() 
		{
			Name = playerName
		};
		New(_playersSaveFolder, playerName, ".bmplr", stateData);
	}
	public bool PlayerExists(string playerName) => Exists(_playersSaveFolder, playerName, ".bmplr");
	public List<PlayerState> LoadAllPlayers() => LoadAll<PlayerState>(_playersSaveFolder, ".bmplr");

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
			WorldHeight = worldHeight,
			Tiles = new ushort[worldWidth * worldHeight]
		};
		GenerateWorldTiles(worldData);
		
		New(_worldsSaveFolder, worldName, ".bmwld", worldData);
	}
	
	public bool WorldExists(string worldName) => Exists(_worldsSaveFolder, worldName, ".bmwld");
	public List<WorldState> LoadAllWorlds() => LoadAll<WorldState>(_worldsSaveFolder, ".bmwld");
		
	public void SelectWorld(WorldState worldState)
	{
		SelectedWorld = worldState;
		BlasterMasterGame.RequestWorldInitialization();
	}
	
	private void GenerateWorldTiles(WorldState worldState) 
	{
		int width = worldState.WorldWidth;
		int height = worldState.WorldHeight;
		
		for (int x = 0; x < width; x++) 
		{
			for (int y = 0; y < height; y++) 
			{
				worldState.SetTile(x, y, BlockID.Stone);
			}
		}
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
	public ushort[] Tiles { get; set; } = Array.Empty<ushort>();
	public int WorldWidth { get; set; }
	public int WorldHeight { get; set; }
	
	public ushort GetTile(int x, int y) 
	{
		return Tiles[y * WorldWidth + x];
	}
	
	 public void SetTile(int x, int y, ushort value)
	{
		Tiles[y * WorldWidth + x] = value;
	}
}