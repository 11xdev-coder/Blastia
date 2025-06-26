using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus;

public class MainMenu(SpriteFont font, bool isActive = true) : Menu(font, isActive)
{
	protected override void AddElements()
	{
		Button singlePlayerButton = new Button(new Vector2(0, 500), "Single Player",
			Font, OnClickSinglePlayer)
		{
			HAlign = 0.5f
		};
		Elements.Add(singlePlayerButton);

		Button multiplayerButton = new Button(new Vector2(0, 550), "Multiplayer",
			Font, OnClickMultiplayer)
		{
			HAlign = 0.5f
		};
		Elements.Add(multiplayerButton);

		Button settingsButton = new Button(new Vector2(0, 600), "Settings",
			Font, OnClickSettings)
		{
			HAlign = 0.5f
		};
		Elements.Add(settingsButton);
		
		Button exitButton = new Button(new Vector2(0, 650), "Exit",
			Font, OnClickExit)
		{
			HAlign = 0.5f
		};
		Elements.Add(exitButton);
	}

	private void OnClickSinglePlayer()
	{
		SwitchToMenu(BlastiaGame.PlayersMenu);
		BlastiaGame.WorldsMenu?.ToggleMultiplayer(false);
		BlastiaGame.PlayersMenu?.ToggleSwitchToJoinMenu(false);
	}

	private void OnClickMultiplayer()
	{
		SwitchToMenu(BlastiaGame.MultiplayerMenu);
		BlastiaGame.WorldsMenu?.ToggleMultiplayer(true);
	}

	private void OnClickSettings()
	{
		SwitchToMenu(BlastiaGame.SettingsMenu);
	}

	private void OnClickExit()
	{
		BlastiaGame.RequestExit();
	}
}