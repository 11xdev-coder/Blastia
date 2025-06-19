using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldsMenu : CollectionMenu
{    
	private bool Host { get; set; }
	
	public WorldsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		
	}

	public void ToggleMultiplayer(bool host)
	{
		Host = host;
		Console.WriteLine($"Worlds menu now host: {host}");
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
	
	protected override IEnumerable<object> LoadItems() => PlayerNWorldManager.Instance.LoadAllWorlds();

	protected override void SelectItem(object worldState)
	{
		PlayerNWorldManager.Instance.SelectWorld((WorldState) worldState, Host);
	}	
}