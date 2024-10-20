using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class PlayersMenu : CollectionMenu
{    
	public PlayersMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		
	}

	protected override string GetCreateButtonLabel() => "New player";
	
	protected override void Create()
	{
		SwitchToMenu(BlasterMasterGame.PlayerCreationMenu);
	}

	protected override void Back()
	{
		SwitchToMenu(BlasterMasterGame.MainMenu);
	}
	
	protected override IEnumerable<object> LoadItems() => PlayerManager.Instance.LoadAllPlayers();

	protected override void SelectItem(object playerState)
	{
		PlayerManager.Instance.SelectPlayer((PlayerState) playerState);
		SwitchToMenu(BlasterMasterGame.WorldsMenu);
	}	
}