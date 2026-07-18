using Blastia.Main.Persistence;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class PlayerCreationMenu : AbstractCreationMenu 
{
    protected override Menu? PreviousMenu => BlastiaGame.GetMenu<PlayerSelectionMenu>();
    
    public PlayerCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive) 
    {
        
    }

    protected override void RandomizeName() => _name?.SetText(RandomNameGenerator.GeneratePlayerName(_name.CharacterLimit));

    protected override void Create()
    {
        if (_name?.Text == null) return;
		string playerName = _name.StringBuilder.ToString();

		SaveValidationResult result = PlayerManager.Instance.NewPlayer(playerName);
		if (result == SaveValidationResult.Success)
        {
            SwitchToMenu(BlastiaGame.GetMenu<PlayerSelectionMenu>());
            return;
        }
        
        ShowErrorMessage(result);
    }
}