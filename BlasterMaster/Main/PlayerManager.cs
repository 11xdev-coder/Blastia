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

    private string GetPlayerPath(string playerName)
    {
        // path/name.bmplr
        return _playersSaveFolder + playerName + ".bmplr";
    }
}