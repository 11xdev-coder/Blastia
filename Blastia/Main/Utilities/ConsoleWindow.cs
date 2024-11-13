using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Blastia.Main.Utilities;

public class ConsoleWindow
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    private Thread _consoleThread;
    private readonly ConcurrentQueue<string> _commandQueue;
    private volatile bool _isRunning; // can be edited in any thread
    
    public ConsoleWindow()
    {
        _commandQueue = new ConcurrentQueue<string>();
        _isRunning = false;
    }
    
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
        if (_consoleThread.IsAlive)
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
            else
            {
                // process commands
                while (_commandQueue.TryDequeue(out string? command))
                {
                    ProcessCommand(command);
                }
                Thread.Sleep(10);
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