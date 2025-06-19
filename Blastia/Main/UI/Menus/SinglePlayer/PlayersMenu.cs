using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class PlayersMenu : CollectionMenu
{    
	public PlayersMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		
	}

	protected override string GetCreateButtonLabel() => "New player";
	
	protected override void Create()
	{
		SwitchToMenu(BlastiaGame.PlayerCreationMenu);
	}

	protected override void Back()
	{
		SwitchToMenu(BlastiaGame.MainMenu);
	}
	
	protected override IEnumerable<object> LoadItems() => PlayerNWorldManager.Instance.LoadAllPlayers();

	protected override void SelectItem(object playerState)
	{
		PlayerNWorldManager.Instance.SelectPlayer((PlayerState) playerState);
		SwitchToMenu(BlastiaGame.WorldsMenu);
	}	
}