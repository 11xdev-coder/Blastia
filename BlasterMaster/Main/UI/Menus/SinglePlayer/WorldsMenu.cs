using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class WorldsMenu : CollectionMenu
{    
	public WorldsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		
	}

	protected override string GetCreateButtonLabel() => "New world";
	
	protected override void Create()
	{
		SwitchToMenu(BlasterMasterGame.WorldCreationMenu);
	}

	protected override void Back()
	{
		SwitchToMenu(BlasterMasterGame.PlayersMenu);
	}
	
	protected override IEnumerable<object> LoadItems() => PlayerManager.Instance.LoadAllWorlds();

	protected override void SelectItem(object worldState)
	{
		PlayerManager.Instance.SelectWorld((WorldState) worldState);
	}	
}