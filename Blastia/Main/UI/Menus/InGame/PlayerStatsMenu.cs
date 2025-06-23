using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class PlayerStatsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Progressbar? _playerHealth;

    protected override void AddElements()
    {
        _playerHealth = new Progressbar(Vector2.Zero, BlastiaGame.ProgressBarBackground)
        {
            HAlign = 0.9f,
            VAlign = 0.05f
        };
        Elements.Add(_playerHealth);
    }

    public void UpdateHealth(float life, float maxLife)
    {
        if (_playerHealth == null) return;

        _playerHealth.Progress = life / maxLife;
    }
}