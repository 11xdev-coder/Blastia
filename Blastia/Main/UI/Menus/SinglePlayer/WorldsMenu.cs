using Blastia.Main.Persistence;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldsMenu : CollectionMenu
{
	public override ActivationMethod ActivationType => ActivationMethod.HideWhenInGame;
	
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
		SwitchToMenu(BlastiaGame.GetMenu<WorldCreationMenu>());
	}

	protected override void Back()
	{
		SwitchToMenu(BlastiaGame.GetMenu<PlayersMenu>());
	}
	
	protected override IEnumerable<object> LoadItems() => WorldManager.Instance.LoadAllWorlds();

	protected override void SelectItem(object worldState)
	{
		var fullyLoadedState = Saving.Load<WorldState>(((WorldState) worldState).FilePath);
		WorldManager.Instance.SelectWorld(fullyLoadedState, Host);
	}	
}