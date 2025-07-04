﻿using System.Numerics;
using Blastia.Main.Networking;
using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI.Menus;

public class MultiplayerMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
        var hostGame = new Button(Vector2.Zero, "Host game", Font, HostGame)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
        };
        Elements.Add(hostGame);
        
        var joinGame = new Button(Vector2.Zero, "Join game", Font, JoinGame)
        {
            HAlign = 0.5f,
            VAlign = 0.55f,
        };
        Elements.Add(joinGame);
        
        var back = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.6f,
        };
        Elements.Add(back);
    }

    private void HostGame()
    {
        // select a player then world
        SwitchToMenu(BlastiaGame.PlayersMenu);
        BlastiaGame.PlayersMenu?.ToggleSwitchToJoinMenu(false);
    }

    private void JoinGame()
    {
        // select player (SwitchToJoinMenu flag) then enter code
        SwitchToMenu(BlastiaGame.PlayersMenu);
        BlastiaGame.PlayersMenu?.ToggleSwitchToJoinMenu(true);
    }

    private void Back()
    {
        SwitchToMenu(BlastiaGame.MainMenu);
    }
}