namespace BlasterMaster.Main;

public class PlayerManager : Singleton<PlayerManager>
{
    private string? _playersSaveFolder;

    public void Initialize(string playersSaveFolder)
    {
        _playersSaveFolder = playersSaveFolder;
    }

    public void NewPlayer()
    {
        if (!string.IsNullOrEmpty(_playersSaveFolder))
        {
            string fileName = _playersSaveFolder + "player.bmplr";
        
            if (Directory.Exists(_playersSaveFolder) && !File.Exists(fileName))
            {
                File.Create(fileName);
            }
        }
    }
}