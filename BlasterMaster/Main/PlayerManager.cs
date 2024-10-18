using System.Text.Json;
using BlasterMaster.Main.Utilities;

namespace BlasterMaster.Main;

public class PlayerManager : Singleton<PlayerManager>
{
	private string? _playersSaveFolder;
	private string? _worldsSaveFolder;
	
	public PlayerState? SelectedPlayer { get; private set; }

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

	private List<T> LoadAll<T>(string? folder, string extension, Func<string, T> newInstanceCreator)
		where T : class
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
					// create new instance with file name
					string name = Path.GetFileNameWithoutExtension(file);
					T item = newInstanceCreator(name);
					items.Add(item);
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
	
	// player methods
	public void NewPlayer(string playerName) => New(_playersSaveFolder, playerName, ".bmplr");
	public bool PlayerExists(string playerName) => Exists(_playersSaveFolder, playerName, ".bmplr");
	public List<PlayerState> LoadAllPlayers() => 
		LoadAll(_playersSaveFolder, ".bmplr", name => new PlayerState { Name = name });

	public void SelectPlayer(PlayerState playerState)
	{
		SelectedPlayer = playerState;
	}
	
	// world methods
	public void NewWorld(string worldName, WorldDifficulty difficulty = WorldDifficulty.Easy) 
	{
		WorldState world = new WorldState { Name = worldName, Difficulty = difficulty };
		New(_worldsSaveFolder, worldName, ".bmwld", world);
	}
	
	public bool WorldExists(string worldName) => Exists(_worldsSaveFolder, worldName, ".bmwld");
	public List<WorldState> LoadAllWorlds() => 
		LoadAll(_worldsSaveFolder, ".bmwld", name => new WorldState { Name = name });
}

[Serializable]
public class PlayerState
{
	public string Name { get; set; } = "";
}

[Serializable]
public class WorldState
{
	public string Name { get; set; } = "";
	public WorldDifficulty Difficulty { get; set; } = WorldDifficulty.Easy;
}