using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldsMenu : CollectionMenu
{    
	public WorldsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		
	}

	protected override string GetCreateButtonLabel() => "New world";
	
	protected override void Create()
	{
		SwitchToMenu(BlastiaGame.WorldCreationMenu);
	}

	protected override void Back()
	{
		SwitchToMenu(BlastiaGame.PlayersMenu);
	}
	
	protected override IEnumerable<object> LoadItems() => PlayerManager.Instance.LoadAllWorlds();

	protected override void SelectItem(object worldState)
	{
		PlayerManager.Instance.SelectWorld((WorldState) worldState);
	}	
}