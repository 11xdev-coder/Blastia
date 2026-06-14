using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Persistence;

[Serializable]
public class PlayerState : State
{
}

public class PlayerManager : Singleton<PlayerManager>
{
    public static string PlayersSaveFolder = Path.Combine(Paths.GetSaveGameDirectory(), "Players");
    public static string Extension = ".blst";
    public PlayerState? PlayerState;
    
	public void NewPlayer(string playerName) 
	{
		PlayerState playerData = new() 
		{
			Name = playerName
		};
		ManagerFileHelper.New(PlayersSaveFolder, playerName, Extension, playerData);
	}
	public bool PlayerExists(string playerName) => ManagerFileHelper.Exists(PlayersSaveFolder, playerName, Extension);
	public List<PlayerState> LoadAllPlayers() => ManagerFileHelper.LoadAll<PlayerState>(PlayersSaveFolder, Extension);

	/// <summary>
	/// Selects the player state
	/// </summary>
	public void SelectPlayer(PlayerState playerState)
	{
		PlayerState = playerState;

			// switchToMenu(BlastiaGame.GetMenu<JoinGameMenu>());
	}
	
	public void UnselectPlayer() => PlayerState = null;
}