using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Blastia.Main.Commands;

public class ConsoleWindow
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    private Thread? _consoleThread;
    private volatile bool _isRunning; // can be edited in any thread

    public void Open(string title)
    {
        AllocConsole();
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
        
        FreeConsole();
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
        switch (command)
        {
            case "help":
                Console.WriteLine("Help command");
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }
}