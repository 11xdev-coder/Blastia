namespace BlasterMaster.Main;

public class PlayerManager : Singleton<PlayerManager>
{
    private string? _playersSaveFolder;

    public void Initialize(string playersSaveFolder)
    {
        _playersSaveFolder = playersSaveFolder;
    }

    public void NewPlayer(string playerName)
    {
        if (!string.IsNullOrEmpty(_playersSaveFolder))
        {
            string fileName = GetPlayerPath(playerName);
        
            if (Directory.Exists(_playersSaveFolder) && !File.Exists(fileName))
            {
                File.Create(fileName);
            }
        }
        else throw new NullReferenceException("Player save path not initialized.");
    }

    /// <summary>
    /// Returns true if player file already exists
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException">If player save path hasn't initialized yet</exception>
    public bool AlreadyExists(string playerName)
    {
        if (!string.IsNullOrEmpty(_playersSaveFolder))
        {
            string fileName = GetPlayerPath(playerName);

            if (File.Exists(fileName)) return true;
        }
        else throw new NullReferenceException("Player save path not initialized.");

        return false;
    }

    public List<PlayerState> LoadAllPlayerStates()
    {
        if (!string.IsNullOrEmpty(_playersSaveFolder))
        {
            List<PlayerState> playerStates = new List<PlayerState>();
            
            // loop through each file in folder
            foreach (string file in Directory.GetFiles(_playersSaveFolder))
            {
                if (file.EndsWith(".bmplr"))
                {
                    // get player name without ".bmplr"
                    string playerName = Path.GetFileNameWithoutExtension(file);
                    
                    // create new player state with custom name
                    PlayerState playerState = new PlayerState
                    {
                        Name = playerName
                    };
                    playerStates.Add(playerState);
                }
            }

            return playerStates;
        }

        return []; // return nothing if no save folder
    }

    private string GetPlayerPath(string playerName)
    {
        // path/name.bmplr
        return _playersSaveFolder + playerName + ".bmplr";
    }
}

[Serializable]
public class PlayerState
{
    public string Name { get; set; } = "";
}