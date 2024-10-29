using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class PlayerCreationMenu : CreationMenu
{
	private PlayerPreview? _playerPreview;
	
	public PlayerCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
	{
		AddElements();
	}

	protected override string GetNameLabel() => "Player name";
	protected override string GetExistsText() => "Player already exists!";
	
	private void AddElements()
	{
		_playerPreview = new PlayerPreview(Vector2.Zero, Font)
		{
			HAlign = 0.7f,
			VAlign = 0.55f
		};
		_playerPreview.AddToElements(Elements);
	}

	protected override void UpdateSpecific()
	{
		if (_playerPreview == null || NameInput?.Text == null) return;
		_playerPreview.Name = NameInput.StringBuilder.ToString();
	}
	
	protected override void Create()
	{
		if (NameInput?.Text == null) return;
		string playerName = NameInput.StringBuilder.ToString();

		if (!PlayerManager.Instance.PlayerExists(playerName))
		{
			// create player if doesnt exist
			PlayerManager.Instance.NewPlayer(NameInput.StringBuilder.ToString());
			
			Back(); // go back
		}
		else
		{
			ShowExistsError();
		}
	}
	
	protected override void Back() => SwitchToMenu(BlastiaGame.PlayersMenu);
}