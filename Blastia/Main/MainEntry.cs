namespace Blastia.Main;

public static class MainEntry
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("-synth"))
        {
            Synthesizer.Synthesizer.Launch([]);
            return;
        }

        bool fullscreen = false;
        if (args.Contains("-fullscreen"))
        {
            fullscreen = true;
        }

        BlastiaGame game = new BlastiaGame(fullscreen);
        game.Run();
    }
}
