using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Blastia.Main.GameState;
using Microsoft.Win32.SafeHandles;

namespace Blastia.Main.Commands;

public class ConsoleWindow
{

    private Thread? _consoleThread;
    private volatile bool _isRunning; // can be edited in any thread

    private World? _world;
    private readonly GameRuleCommands _gameRules = new();

    public void InitializeWorldCommands(World world)
    {
        _world = world;
        _gameRules.AddGameRule("ruler", b =>
        {
            world.RulerMode = b;
        }, () => world.RulerMode);
        _gameRules.AddGameRule("draw_collision_grid", b => world.DrawCollisionGrid = b, () => world.DrawCollisionGrid);
        Console.WriteLine("Commands initialized");
    }

    public void UnloadWorldCommands()
    {
        _gameRules.Clear();
        _world = null;
        Console.WriteLine("Commands unloaded");
    }
    
    public void Open(string title)
    {
        Console.Title = title;
        
        _isRunning = true;
        _consoleThread = new Thread(InputLoop);
        _consoleThread.Start();
    }

    public void Close()
    {
        _isRunning = false;
        if (_consoleThread is {IsAlive: true})
        {
            _consoleThread.Join();
        }
    }

    /// <summary>
    /// While console is running, tries to get input and enqueue it
    /// </summary>
    private void InputLoop()
    {
        Console.WriteLine("Blastia Game Console");
        Console.WriteLine("Type 'help' for a list of available commands");
        
        while (_isRunning)
        {
            if (Console.KeyAvailable)
            {
                string? input = Console.ReadLine()?.Trim().ToLower();

                // queue command for further processing
                if (!string.IsNullOrEmpty(input))
                {
                    ProcessCommand(input);
                }
            }
        }
    }

    private void ProcessCommand(string command)
    {
        var commandList = command.Split(" ");
        var commandName = commandList[0];

        if (_gameRules.HasGameRule(commandName))
        {   
            // valid gamerule + flag
            if (commandList.Length == 2)
            {
                var flag = commandList[1];
                if (int.TryParse(flag, out var intFlag))
                {
                    _gameRules.SetGameRule(commandName, intFlag);
                }
                else if (bool.TryParse(flag, out var boolFlag))
                {
                    _gameRules.SetGameRule(commandName, boolFlag);
                }
                else
                {
                    Console.WriteLine("Couldn't parse gamerule flag");
                }
            }
            else // valid gamerule (no flag)
            {
                _gameRules.SetGameRule(commandName);
            }
        }
        
        // switch (command)
        // {
        //     case "help":
        //         Console.WriteLine("Help command");
        //         break;
        //     default:
        //         Console.WriteLine($"Unknown command: {command}");
        //         break;
        // }
    }
}