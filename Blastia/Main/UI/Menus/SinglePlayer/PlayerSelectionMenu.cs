using Blastia.Main.Persistence;
using Blastia.Main.UI.Buttons;
using Blastia.Main.UI.Menus.Multiplayer;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class PlayerSelectionItem : AbstractSelectionItem<PlayerState>
{
	private Action _switchMenuOnSelect;
	
    public PlayerSelectionItem(Vector2 position, PlayerState playerState, SpriteFont font, Action switchMenuOnSelect) : base(position, playerState, font)
	{
		_switchMenuOnSelect = switchMenuOnSelect;
	}
    
	protected override void LoadState()
	{
		var fullyLoadedState = Saving.Load<PlayerState>(State.FilePath);
		PlayerManager.Instance.SelectPlayer(fullyLoadedState);
		_switchMenuOnSelect();
	}
}

public class PlayerSelectionMenu : AbstractSelectionMenu<PlayerState>
{
	private bool SwitchToJoinMenu { get; set; }
	protected override string TopText => "Player Select";
	protected override Menu? CreationMenu => BlastiaGame.GetMenu<PlayerCreationMenu>();
	protected override Menu? PreviousMenu => BlastiaGame.GetMenu<MainMenu>();

	public PlayerSelectionMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
    }

    protected override List<PlayerState> GetStates() => PlayerManager.Instance.LoadAllPlayers();
    protected override PlayerSelectionItem CreateSelectionItem(PlayerState state) => new PlayerSelectionItem(Vector2.Zero, state, Font, SwitchMenuOnSelect);

	private void SwitchMenuOnSelect() => SwitchToMenu(SwitchToJoinMenu ? BlastiaGame.GetMenu<JoinGameMenu>() : BlastiaGame.GetMenu<WorldSelectionMenu>());

    protected override bool DeleteState(string filePath) => WorldManager.Instance.DeleteWorld(filePath);
    
    public void ToggleSwitchToJoinMenu(bool switchToJoinMenu)
	{
		SwitchToJoinMenu = switchToJoinMenu;
		Console.WriteLine($"Players menu now switches to join menu: {SwitchToJoinMenu}");
	}
}