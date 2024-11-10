using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Blastia.Main.Utilities;

public static class ConsoleHelper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool WriteConsoleW(
        IntPtr hConsoleOutput,
        string lpBuffer,
        uint nNumberOfCharsToWrite,
        out uint lpNumberOfCharsWritten,
        IntPtr lpReserved);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr handle, out uint lpMode);

    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;
    private const uint DisableNewlineAutoReturn = 0x0008;

    private static IntPtr _consoleOutputHandle;
    private static bool _isInitialized;
    private static readonly object ConsoleLock = new();

    public static void CreateConsole(string title)
    {
        if (_isInitialized) return;

        try
        {
            if (!AllocConsole())
            {
                throw new InvalidOperationException($"AllocConsole failed with error: {Marshal.GetLastWin32Error()}");
            }

            _consoleOutputHandle = GetStdHandle(StdOutputHandle);
            if (_consoleOutputHandle == IntPtr.Zero || _consoleOutputHandle == new IntPtr(-1))
            {
                throw new InvalidOperationException($"GetStdHandle failed with error: {Marshal.GetLastWin32Error()}");
            }

            // Enable virtual terminal processing
            if (!GetConsoleMode(_consoleOutputHandle, out uint mode))
            {
                throw new InvalidOperationException($"GetConsoleMode failed with error: {Marshal.GetLastWin32Error()}");
            }

            mode |= EnableVirtualTerminalProcessing | DisableNewlineAutoReturn;
            if (!SetConsoleMode(_consoleOutputHandle, mode))
            {
                throw new InvalidOperationException($"SetConsoleMode failed with error: {Marshal.GetLastWin32Error()}");
            }

            // Redirect standard output streams
            var standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(standardOutput);

            Console.Title = title;
            _isInitialized = true;

            // Test write
            WriteLine("Console initialized successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Console initialization failed: {ex}");
            throw;
        }
    }

    public static void WriteLine(string text)
    {
        if (!_isInitialized || _consoleOutputHandle == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("Console not initialized");
            return;
        }

        lock (ConsoleLock)
        {
            try
            {
                text += "\n";
                uint written;
                if (!WriteConsoleW(_consoleOutputHandle, text, (uint)text.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"WriteConsole failed with error code: {error}");
                }

                if (written != text.Length)
                {
                    throw new InvalidOperationException($"WriteConsole wrote {written} chars instead of {text.Length}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WriteLine failed: {ex}");
                // Fallback to standard console output
                Console.WriteLine(text);
            }
        }
    }

    public static void RemoveConsole()
    {
        if (!_isInitialized) return;

        try
        {
            _isInitialized = false;
            _consoleOutputHandle = IntPtr.Zero;
            FreeConsole();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RemoveConsole failed: {ex}");
        }
    }

    /// <summary>
    /// While console is running, tries to get input and enqueue it
    /// </summary>
    /// <param name="isRunning">Flag is console thread running</param>
    /// <param name="commandQueue">Queue of commands to process later</param>
    public static void ConsoleInputLoop(ref bool isRunning, ConcurrentQueue<string> commandQueue)
    {
        WriteLine("Blastia Game Console");
        WriteLine("Type 'help' for a list of available commands");
        
        while (isRunning)
        {
            string? input = Console.ReadLine()?.Trim().ToLower();
            // queue command for further processing
            if (!string.IsNullOrEmpty(input))
            {
                commandQueue.Enqueue(input);
            }
        }
    }

    /// <summary>
    /// Tries dequeuing command queue and processes it 
    /// </summary>
    /// <param name="commandQueue"></param>
    public static void UpdateConsole(ConcurrentQueue<string> commandQueue)
    {
        while (commandQueue.TryDequeue(out string? command))
        {
            if (!string.IsNullOrEmpty(command))
            {
                ProcessCommand(command);
            }
        }
    }

    private static void ProcessCommand(string command)
    {
        switch (command)
        {
            case "help":
                WriteLine("Help command");
                break;
            default:
                WriteLine($"Unknown command: {command}");
                break;
        }
    }
}