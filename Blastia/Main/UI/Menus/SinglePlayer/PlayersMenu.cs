using Blastia.Main.Persistence;
using Blastia.Main.UI.Menus.Multiplayer;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class PlayersMenu : CollectionMenu
{    
	/// <summary>
	/// If true, after selecting a player, the menu will switch to <see cref="Blastia.Main.UI.Menus.Multiplayer.JoinGameMenu"/>
	/// </summary>
	private bool SwitchToJoinMenu { get; set; }
	
	public PlayersMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		
	}
	
	public void ToggleSwitchToJoinMenu(bool switchToJoinMenu)
	{
		SwitchToJoinMenu = switchToJoinMenu;
		Console.WriteLine($"Players menu now switches to join menu: {SwitchToJoinMenu}");
	}

	protected override string GetCreateButtonLabel() => "New player";
	
	protected override void Create()
	{
		SwitchToMenu(BlastiaGame.GetMenu<PlayerCreationMenu>());
	}

	protected override void Back()
	{
		SwitchToMenu(BlastiaGame.GetMenu<MainMenu>());
	}
	
	protected override IEnumerable<object> LoadItems() => PlayerManager.Instance.LoadAllPlayers();

	protected override void SelectItem(object playerState)
	{
		PlayerManager.Instance.SelectPlayer((PlayerState) playerState);
		
		if (!SwitchToJoinMenu)
			SwitchToMenu(BlastiaGame.GetMenu<WorldSelectionMenu>());
		else
			SwitchToMenu(BlastiaGame.GetMenu<JoinGameMenu>());
	}	
}