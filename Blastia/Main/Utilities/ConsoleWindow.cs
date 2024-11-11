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

    public void Open(string title)
    {
        AllocConsole();
        Console.Title = title;
    }

    public void Close()
    {
        FreeConsole();
    }

    /// <summary>
    /// While console is running, tries to get input and enqueue it
    /// </summary>
    /// <param name="isRunning">Flag is console thread running</param>
    public void InputLoop(ref bool isRunning)
    {
        Console.WriteLine("Blastia Game Console");
        Console.WriteLine("Type 'help' for a list of available commands");
        
        while (isRunning)
        {
            string? input = Console.ReadLine()?.Trim().ToLower();
            // queue command for further processing
            if (!string.IsNullOrEmpty(input))
            {
                ProcessCommand(input);
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