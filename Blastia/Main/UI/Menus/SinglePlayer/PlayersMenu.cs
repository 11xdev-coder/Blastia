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
		SwitchToMenu(BlastiaGame.PlayerCreationMenu);
	}

	protected override void Back()
	{
		SwitchToMenu(BlastiaGame.MainMenu);
	}
	
	protected override IEnumerable<object> LoadItems() => PlayerNWorldManager.Instance.LoadAllPlayers();

	protected override void SelectItem(object playerState)
	{
		PlayerNWorldManager.Instance.SelectPlayer((PlayerState) playerState, SwitchToJoinMenu, SwitchToMenu);
		
		if (!SwitchToJoinMenu)
			SwitchToMenu(BlastiaGame.WorldsMenu);
	}	
}